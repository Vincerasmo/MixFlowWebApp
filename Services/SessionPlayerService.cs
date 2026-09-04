using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Constants;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.SessionPlayerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public static class SessionPlayerStatus
    {
        public const string CheckedIn = "CheckedIn";
        public const string Benched = "Benched";
    }

    public class SessionPlayerService : ISessionPlayerService
    {
        private readonly MixFlowDbContext _context;
        private readonly IMatchService _matchService;
        private readonly ILogger<SessionPlayerService> _logger;

        public SessionPlayerService(MixFlowDbContext context, IMatchService matchService, ILogger<SessionPlayerService> logger)
        {
            _context = context;
            _matchService = matchService;
            _logger = logger;
        }

        /// Add a player to a session (check-in).
        public async Task<SessionPlayer?> AddPlayerToSessionAsync(int sessionId, int playerId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null) return null;

            var player = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null) return null;

            // A session can only ever draw from its own organizer's roster — otherwise
            // one organizer's session could pull in another organizer's player just by
            // guessing/incrementing a player ID.
            if (player.OrganizerId != session.OrganizerId)
                throw new InvalidOperationException("This player doesn't belong to this organizer's roster.");

            var existing = await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (existing != null) return existing;

            // A player can only be checked into one Active session at a time. Without this,
            // someone could end up queued, benched, or mid-match in two live sessions
            // simultaneously — which corrupts queue state, stats, and match assignment
            // for both sessions at once.
            var alreadyInAnotherActiveSession = await _context.SessionPlayers
                .Include(sp => sp.Session)
                .AnyAsync(sp => sp.PlayerId == playerId
                             && sp.SessionId != sessionId
                             && sp.Session.Status == SessionStatus.Active);

            if (alreadyInAnotherActiveSession)
                throw new InvalidOperationException("This player is already checked into another active session.");

            var sessionPlayer = new SessionPlayer
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Status = SessionPlayerStatus.CheckedIn,
                CheckInTime = DateTime.UtcNow,
            };

            _context.SessionPlayers.Add(sessionPlayer);
            await _context.SaveChangesAsync();

            // 🐛 FIX: this call went missing at some point — AddPlayerToSessionAsync was
            // only ever setting Status = "CheckedIn" and stopping there. With no "add to
            // queue" action left anywhere in the UI (that flow was removed on the
            // assumption checking a player in AND queuing them were the same action now),
            // a checked-in player had no path into the queue at all — they'd show up fine
            // in "Manage Players" but never appear on the Queue page.
            //
            // Also logs instead of silently swallowing, unlike before — if this ever fails
            // again (e.g. session isn't Active yet), it'll show up in the logs instead of
            // vanishing without a trace.
            try
            {
                await _matchService.EnqueuePlayerAsync(sessionId, playerId);
                await _context.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Player {PlayerId} added to Session {SessionId} but couldn't be auto-enqueued", playerId, sessionId);
            }

            return await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionPlayerId == sessionPlayer.SessionPlayerId);
        }

        /// Bench a player in a session. Benching also breaks any lock pair they're in —
        /// a benched player can't stay locked to a partner who's still playing.
        public async Task<SessionPlayer?> BenchPlayerAsync(int sessionId, int playerId, string reason)
        {
            var sp = await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null || sp.Status == SessionPlayerStatus.Benched) return null;

            if (sp.LockedPartnerId.HasValue)
            {
                await UnlockPairAsync(sessionId, playerId);
            }

            sp.Status = SessionPlayerStatus.Benched;
            sp.BenchReason = reason;
            sp.BenchedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return sp;
        }

        /// Return a player from bench to checked-in.
        public async Task<SessionPlayer?> ReturnFromBenchAsync(int sessionId, int playerId)
        {
            var sp = await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null || sp.Status == SessionPlayerStatus.CheckedIn) return null;

            sp.Status = SessionPlayerStatus.CheckedIn;
            sp.BenchReason = null;
            sp.BenchedAt = null;

            await _context.SaveChangesAsync();
            return sp;
        }

        /// Remove a player from a session. Also breaks their lock pair, if any, so their
        /// former partner isn't left pointing at a player no longer in this session.
        public async Task<bool> RemovePlayerFromSessionAsync(int sessionId, int playerId)
        {
            var sp = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null) return false;

            // Don't allow removing someone currently on a court or about to start one —
            // that would silently pull them out of a match nobody chose to end.
            var inActiveOrReadyMatch = await _context.MatchPlayers
                .AnyAsync(mp => mp.PlayerId == playerId
                             && mp.Match.SessionId == sessionId
                             && mp.Match.Status != MatchStatus.Completed);
            if (inActiveOrReadyMatch)
                throw new InvalidOperationException("This player is currently in a match (or about to start one) and can't be removed. Finish or record that match first.");

            // Don't allow removing someone who's already played — their completed match
            // history for this session should stay attributable to a real roster entry.
            var hasCompletedMatch = await _context.MatchPlayers
                .AnyAsync(mp => mp.PlayerId == playerId
                             && mp.Match.SessionId == sessionId
                             && mp.Match.Status == MatchStatus.Completed);
            if (hasCompletedMatch)
                throw new InvalidOperationException("This player has already played a match in this session and can't be removed from the roster.");

            if (sp.LockedPartnerId.HasValue)
            {
                await UnlockPairAsync(sessionId, playerId);
            }

            // Same cleanup BenchPlayerAsync already relies on — pulls them out of the queue
            // if they were "Waiting", so their name doesn't keep appearing there after removal.
            await _matchService.RemoveFromQueueAsync(sessionId, playerId);

            _context.SessionPlayers.Remove(sp);
            await _context.SaveChangesAsync();
            return true;
        }

        /// Session players enriched with games-played-in-this-session and lock-pair info.
        public async Task<List<SessionPlayerDto>> GetSessionPlayersWithStatsAsync(int sessionId)
        {
            var sessionPlayers = await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId)
                .Include(sp => sp.Player)
                .ToListAsync();

            var gamesPlayedMap = await _context.MatchPlayers
                .Where(mp => mp.Match.SessionId == sessionId)
                .GroupBy(mp => mp.PlayerId)
                .Select(g => new { PlayerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlayerId, x => x.Count);

            var nameByPlayerId = sessionPlayers.ToDictionary(sp => sp.PlayerId, sp => sp.Player.FullName);

            return sessionPlayers.Select(sp => new SessionPlayerDto
            {
                SessionPlayerId = sp.SessionPlayerId,
                PlayerId = sp.PlayerId,
                FullName = sp.Player.FullName,
                SkillCategory = sp.Player.SkillCategory,
                SkillLevel = sp.Player.SkillLevel,
                Status = sp.Status,
                BenchReason = sp.BenchReason,
                CheckInTime = sp.CheckInTime,
                BenchedAt = sp.BenchedAt,
                GamesPlayedInSession = gamesPlayedMap.TryGetValue(sp.PlayerId, out var count) ? count : 0,
                LockedPartnerId = sp.LockedPartnerId,
                LockedPartnerName = sp.LockedPartnerId.HasValue && nameByPlayerId.TryGetValue(sp.LockedPartnerId.Value, out var partnerName)
                    ? partnerName
                    : null,
            }).ToList();
        }

        /// Lock two players together as a fixed pair for this session. If both players are
        /// already sitting in the "next up" queue (or one is queued and the other still
        /// waiting), they're immediately reorganized so the lock takes effect right away —
        /// not just the next time the queue happens to be rebuilt from scratch.
        public async Task<bool> LockPairAsync(int sessionId, int playerId, int partnerId)
        {
            if (playerId == partnerId) return false;

            var a = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);
            var b = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == partnerId);

            if (a == null || b == null) return false;
            if (a.Status == SessionPlayerStatus.Benched || b.Status == SessionPlayerStatus.Benched) return false;
            if (a.LockedPartnerId.HasValue || b.LockedPartnerId.HasValue) return false;

            a.LockedPartnerId = partnerId;
            b.LockedPartnerId = playerId;

            await _context.SaveChangesAsync();

            await TryPairLockedPartnersImmediatelyAsync(sessionId, playerId, partnerId);

            return true;
        }

        /// Unlock a player from their current pair. Clears both sides symmetrically.
        public async Task<bool> UnlockPairAsync(int sessionId, int playerId)
        {
            var a = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (a == null || !a.LockedPartnerId.HasValue) return false;

            var partnerId = a.LockedPartnerId.Value;
            var b = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == partnerId);

            a.LockedPartnerId = null;
            if (b != null) b.LockedPartnerId = null;

            await _context.SaveChangesAsync();
            return true;
        }

        // ---------------- Locked-pair reorganization ----------------

        /// If both newly-locked players are already sitting in a "next up" (Ready) match
        /// and/or the queue — but not yet actually playing — pull whichever one(s) are
        /// needed so they land on the same team of the same match. Matches that are
        /// currently Active (on a court, in progress) are never touched; if either player
        /// is mid-game the lock simply takes effect the next time they're re-queued.
        private async Task TryPairLockedPartnersImmediatelyAsync(int sessionId, int playerId, int partnerId)
        {
            var openMatches = await _context.Matches
                .Where(m => m.SessionId == sessionId && (m.Status == MatchStatus.Ready || m.Status == MatchStatus.Active))
                .Include(m => m.MatchPlayers)
                .ToListAsync();

            Match? FindMatchOf(int pid) => openMatches.FirstOrDefault(m => m.MatchPlayers.Any(mp => mp.PlayerId == pid));

            var matchA = FindMatchOf(playerId);
            var matchB = FindMatchOf(partnerId);

            // Can't touch a match that's already being played.
            if (matchA?.Status == MatchStatus.Active || matchB?.Status == MatchStatus.Active) return;

            // Already together.
            if (matchA != null && matchB != null && matchA.MatchId == matchB.MatchId) return;

            // Neither is placed in a next-up match yet, so there's nothing to reorganize —
            // the normal next-up builder will pair them together once it next runs.
            if (matchA == null && matchB == null) return;

            var targetMatch = matchA ?? matchB!;
            var otherMatch = matchA != null ? matchB : matchA;
            var incomingPlayerId = matchA != null ? partnerId : playerId;
            var stayingPlayerId = matchA != null ? playerId : partnerId;

            var incomingInQueue = otherMatch == null && await _context.QueueEntries
                .AnyAsync(q => q.SessionId == sessionId && q.PlayerId == incomingPlayerId && q.Status == "Waiting");

            if (otherMatch == null && !incomingInQueue) return;

            var stayingSlot = targetMatch.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == stayingPlayerId);
            if (stayingSlot == null) return;

            var bumpedSlot = targetMatch.MatchPlayers.FirstOrDefault(mp => mp.TeamNumber == stayingSlot.TeamNumber && mp.PlayerId != stayingPlayerId);
            if (bumpedSlot == null) return;

            var bumpedPlayerId = bumpedSlot.PlayerId;

            if (otherMatch != null)
            {
                var incomingSlot = otherMatch.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == incomingPlayerId);
                if (incomingSlot == null) return;

                otherMatch.MatchPlayers.Remove(incomingSlot);
                _context.MatchPlayers.Remove(incomingSlot);
            }
            else
            {
                var incomingQueueEntry = await _context.QueueEntries
                    .FirstAsync(q => q.SessionId == sessionId && q.PlayerId == incomingPlayerId && q.Status == "Waiting");
                incomingQueueEntry.Status = "InMatch";
                incomingQueueEntry.Position = null;
            }

            // Reuse the bumped player's row for the incoming player, rather than
            // add/remove, so the pair ends up on the same team as stayingPlayerId.
            bumpedSlot.PlayerId = incomingPlayerId;

            await ReturnPlayerToWaitingQueueAsync(sessionId, bumpedPlayerId);

            // Pulling the incoming player out of their old ready match leaves it one
            // player short — backfill it from the queue, or dissolve it if no one's free.
            if (otherMatch != null)
            {
                await BackfillOrDissolveMatchAsync(sessionId, otherMatch);
            }

            await _context.SaveChangesAsync();
        }

        private async Task BackfillOrDissolveMatchAsync(int sessionId, Match match)
        {
            var lockedPartnerByPlayerId = await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId && sp.LockedPartnerId != null)
                .ToDictionaryAsync(sp => sp.PlayerId, sp => sp.LockedPartnerId!.Value);

            var waiting = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .OrderByDescending(q => q.PriorityScore ?? 0m)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();

            // Prefer a player who isn't locked to anyone, so backfilling this seat doesn't
            // just split off yet another locked pair.
            var pick = waiting.FirstOrDefault(q => !lockedPartnerByPlayerId.ContainsKey(q.PlayerId))
                       ?? waiting.FirstOrDefault();

            if (pick == null)
            {
                // No one available to fill the last seat — dissolve the match and send
                // everyone still in it back to the queue.
                foreach (var mp in match.MatchPlayers.ToList())
                {
                    _context.MatchPlayers.Remove(mp);
                    await ReturnPlayerToWaitingQueueAsync(sessionId, mp.PlayerId);
                }
                _context.Matches.Remove(match);
                return;
            }

            var team1Count = match.MatchPlayers.Count(mp => mp.TeamNumber == 1);
            var team2Count = match.MatchPlayers.Count(mp => mp.TeamNumber == 2);
            var teamNumber = team1Count <= team2Count ? 1 : 2;

            _context.MatchPlayers.Add(new MatchPlayer { MatchId = match.MatchId, PlayerId = pick.PlayerId, TeamNumber = teamNumber });
            pick.Status = "InMatch";
            pick.Position = null;
        }

        // Mirrors MatchService's queue upsert: reuses the player's existing QueueEntry row
        // (QueueEntry has a unique SessionId+PlayerId index) instead of risking a duplicate
        // insert if they already have a non-"Waiting" row for this session.
        private async Task ReturnPlayerToWaitingQueueAsync(int sessionId, int playerId)
        {
            var existing = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerId);

            if (existing != null)
            {
                existing.Status = "Waiting";
                existing.CheckInTime = DateTime.UtcNow;
                existing.Position = null;
                return;
            }

            _context.QueueEntries.Add(new QueueEntry
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Status = "Waiting",
                CheckInTime = DateTime.UtcNow
            });
        }
    }
}