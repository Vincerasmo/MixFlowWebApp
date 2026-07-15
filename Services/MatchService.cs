using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Constants;
using MixFlowWebApp.Data;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public class MatchService : IMatchService
    {
        // How many prepared-but-not-started matches the "next up" queue tries to keep
        // ready at all times, so the organizer always has a couple of cards to review
        // or edit before they hit a court.
        private const int TargetReadyMatchCount = 2;

        private readonly MixFlowDbContext _context;
        private readonly ILeaderboardService _leaderboardService;

        public MatchService(MixFlowDbContext context, ILeaderboardService leaderboardService)
        {
            _context = context;
            _leaderboardService = leaderboardService;
        }

        // ---------------- Queue Management ----------------

        public async Task<QueueEntry?> EnqueuePlayerAsync(int sessionId, int playerId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active")
                throw new InvalidOperationException("Session not found or not active.");

            var sp = await _context.SessionPlayers
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.PlayerId == playerId && x.Status != "Benched");
            if (sp == null)
                throw new InvalidOperationException("Player is not checked into this session or is benched.");

            var alreadyInMatch = await _context.MatchPlayers
                .AnyAsync(mp => mp.PlayerId == playerId && mp.Match.SessionId == sessionId && mp.Match.Status != MatchStatus.Completed);
            if (alreadyInMatch)
                throw new InvalidOperationException("Player is currently in a match.");

            return await UpsertWaitingQueueEntryAsync(sessionId, playerId);
        }

        public async Task<List<QueueEntry>> GetCurrentQueueAsync(int sessionId)
        {
            return await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderBy(q => q.Position)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();
        }

        public async Task UpdateQueuePrioritiesAsync(int sessionId)
        {
            var waiting = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .ToListAsync();

            foreach (var q in waiting)
            {
                q.PriorityScore = CalculatePriority(q);
            }

            var ordered = waiting
                .OrderByDescending(q => q.PriorityScore ?? 0m)
                .ThenBy(q => q.CheckInTime)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Position = i + 1;
            }
        }

        public async Task<bool> RemoveFromQueueAsync(int sessionId, int playerId)
        {
            var entry = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerId && q.Status == "Waiting");

            if (entry == null) return false;

            entry.Status = "Removed";
            entry.Position = null;

            return true;
        }

        // Puts a player back into the "Waiting" queue, reusing their existing QueueEntry
        // row if they already have one for this session instead of inserting a new row.
        // QueueEntry has a unique (SessionId, PlayerId) index, so a naive insert-if-not-
        // "Waiting" check (as this used to be) throws a DB conflict — and therefore a 500 —
        // the moment a player who already has a non-"Waiting" row (e.g. "Removed" from a
        // bench, or "InMatch" from a prior match) gets queued again.
        private async Task<QueueEntry> UpsertWaitingQueueEntryAsync(int sessionId, int playerId)
        {
            var existing = await _context.QueueEntries
                .Include(q => q.Player)
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerId);

            if (existing != null)
            {
                existing.Status = "Waiting";
                existing.CheckInTime = DateTime.UtcNow;
                existing.Position = null;
                return existing;
            }

            var entry = new QueueEntry
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Status = "Waiting",
                CheckInTime = DateTime.UtcNow
            };

            _context.QueueEntries.Add(entry);
            await _context.SaveChangesAsync();

            return await _context.QueueEntries
                .Include(q => q.Player)
                .FirstAsync(q => q.QueueId == entry.QueueId);
        }

        // ---------------- Match Creation (randomized only) ----------------

        public async Task AutoFillCourtsFromQueueAsync(int sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active") return;

            // Send whatever's already prepared to any free courts, top the next-up
            // pool back up from the queue, then promote again in case the newly built
            // matches can immediately cover courts that were still free.
            await PromoteReadyMatchesToCourtsAsync(sessionId);
            await EnsureReadyMatchesAsync(sessionId);
            await PromoteReadyMatchesToCourtsAsync(sessionId);
        }

        // Fills exactly the requested court — never touches any other court, unlike
        // AutoFillCourtsFromQueueAsync which fills every free court it can.
        public async Task<Match?> CreateMatchForCourtAsync(int sessionId, int courtNumber)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active")
                throw new InvalidOperationException("Session not found or not active.");

            if (courtNumber < 1 || courtNumber > session.NumberOfCourts)
                throw new InvalidOperationException($"Court {courtNumber} is out of range for this session.");

            var courtOccupied = await _context.Matches
                .AnyAsync(m => m.SessionId == sessionId && m.CourtNumber == courtNumber && m.Status == MatchStatus.Active);
            if (courtOccupied)
                throw new InvalidOperationException($"Court {courtNumber} already has a match in progress.");

            // Prefer a match that's already prepared and waiting in the next-up queue —
            // send it straight to this court instead of building a brand new one.
            var readyMatch = await _context.Matches
                .Where(m => m.SessionId == sessionId && m.Status == MatchStatus.Ready)
                .OrderBy(m => m.MatchId)
                .FirstOrDefaultAsync();

            if (readyMatch != null)
            {
                readyMatch.CourtNumber = courtNumber;
                readyMatch.Status = MatchStatus.Active;
                readyMatch.StartTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await EnsureReadyMatchesAsync(sessionId);

                return readyMatch;
            }

            // Nothing prepared — build one directly for this court from the queue.
            await UpdateQueuePrioritiesAsync(sessionId);

            var queueEntries = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderByDescending(q => q.PriorityScore)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();

            var lockedPartnerByPlayerId = await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId && sp.LockedPartnerId != null)
                .ToDictionaryAsync(sp => sp.PlayerId, sp => sp.LockedPartnerId!.Value);

            var selectedPlayers = PickFourForNextCourt(queueEntries, lockedPartnerByPlayerId);
            if (selectedPlayers.Count < 4) return null;

            var match = new Match
            {
                SessionId = sessionId,
                CourtNumber = courtNumber,
                RotationMode = "RandomMix",
                Status = MatchStatus.Active,
                StartTime = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            AssignTeams(match, selectedPlayers, lockedPartnerByPlayerId);

            foreach (var entry in queueEntries.Where(q => selectedPlayers.Any(p => p.PlayerId == q.PlayerId)))
            {
                entry.Status = "InMatch";
                entry.Position = null;
            }

            await _context.SaveChangesAsync();
            await EnsureReadyMatchesAsync(sessionId);

            return match;
        }

        // ---------------- Next Up (Ready) matches ----------------

        public async Task<List<Match>> GetNextUpMatchesAsync(int sessionId)
        {
            return await _context.Matches
                .Where(m => m.SessionId == sessionId && m.Status == MatchStatus.Ready)
                .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
                .OrderBy(m => m.MatchId)
                .ToListAsync();
        }

        public async Task<Match> SwapMatchTeamsAsync(int sessionId, int matchId, int playerAId, int playerBId)
        {
            if (playerAId == playerBId)
                throw new InvalidOperationException("Choose two different players to swap.");

            var match = await _context.Matches
                .Include(m => m.MatchPlayers)
                .FirstOrDefaultAsync(m => m.MatchId == matchId && m.SessionId == sessionId);

            if (match == null)
                throw new InvalidOperationException("Match not found.");
            if (match.Status == MatchStatus.Completed)
                throw new InvalidOperationException("A completed match can't be edited.");

            var mpA = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == playerAId);
            var mpB = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == playerBId);

            if (mpA == null || mpB == null)
                throw new InvalidOperationException("Both players must currently be in this match.");
            if (mpA.TeamNumber == mpB.TeamNumber)
                throw new InvalidOperationException("Those two players are already on the same team.");

            // Locking is symmetric (both sides point at each other), so checking one
            // direction is enough.
            var lockedToEachOther = await _context.SessionPlayers
                .AnyAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerAId && sp.LockedPartnerId == playerBId);
            if (lockedToEachOther)
                throw new InvalidOperationException("These two players are locked as partners — unlock them first if you want to split them onto opposite teams.");

            (mpA.TeamNumber, mpB.TeamNumber) = (mpB.TeamNumber, mpA.TeamNumber);

            await _context.SaveChangesAsync();

            return await _context.Matches
                .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
                .FirstAsync(m => m.MatchId == match.MatchId);
        }

        public async Task<Match> SwapMatchWithQueueAsync(int sessionId, int matchId, int playerOutId, int playerInId)
        {
            if (playerOutId == playerInId)
                throw new InvalidOperationException("Choose two different players to swap.");

            var match = await _context.Matches
                .Include(m => m.MatchPlayers)
                .FirstOrDefaultAsync(m => m.MatchId == matchId && m.SessionId == sessionId);

            if (match == null)
                throw new InvalidOperationException("Match not found.");
            if (match.Status == MatchStatus.Completed)
                throw new InvalidOperationException("A completed match can't be edited.");

            var outgoing = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == playerOutId);
            if (outgoing == null)
                throw new InvalidOperationException("That player isn't currently in this match.");

            if (match.MatchPlayers.Any(mp => mp.PlayerId == playerInId))
                throw new InvalidOperationException("That player is already in this match.");

            var incomingQueueEntry = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerInId && q.Status == "Waiting");
            if (incomingQueueEntry == null)
                throw new InvalidOperationException("That player isn't currently waiting in the queue.");

            var teamNumber = outgoing.TeamNumber;

            _context.MatchPlayers.Remove(outgoing);
            _context.MatchPlayers.Add(new MatchPlayer { MatchId = match.MatchId, PlayerId = playerInId, TeamNumber = teamNumber });

            incomingQueueEntry.Status = "InMatch";
            incomingQueueEntry.Position = null;

            await UpsertWaitingQueueEntryAsync(sessionId, playerOutId);

            await _context.SaveChangesAsync();

            return await _context.Matches
                .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
                .FirstAsync(m => m.MatchId == match.MatchId);
        }

        // Builds up to TargetReadyMatchCount "Ready" matches from the queue (no court
        // assigned yet). Safe to call any time — it only tops the pool up, never removes
        // from it.
        private async Task EnsureReadyMatchesAsync(int sessionId)
        {
            var readyCount = await _context.Matches
                .CountAsync(m => m.SessionId == sessionId && m.Status == MatchStatus.Ready);

            if (readyCount >= TargetReadyMatchCount) return;

            await UpdateQueuePrioritiesAsync(sessionId);

            var queueEntries = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderByDescending(q => q.PriorityScore)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();

            var lockedPartnerByPlayerId = await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId && sp.LockedPartnerId != null)
                .ToDictionaryAsync(sp => sp.PlayerId, sp => sp.LockedPartnerId!.Value);

            var remainingQueue = new List<QueueEntry>(queueEntries);

            while (readyCount < TargetReadyMatchCount)
            {
                var selectedPlayers = PickFourForNextCourt(remainingQueue, lockedPartnerByPlayerId);
                if (selectedPlayers.Count < 4) break;

                var match = new Match
                {
                    SessionId = sessionId,
                    CourtNumber = null,
                    RotationMode = "RandomMix",
                    Status = MatchStatus.Ready,
                    IsCompleted = false
                };

                _context.Matches.Add(match);
                await _context.SaveChangesAsync();

                AssignTeams(match, selectedPlayers, lockedPartnerByPlayerId);

                foreach (var entry in remainingQueue.Where(q => selectedPlayers.Any(p => p.PlayerId == q.PlayerId)))
                {
                    entry.Status = "InMatch";
                    entry.Position = null;
                }

                remainingQueue.RemoveAll(q => selectedPlayers.Any(p => p.PlayerId == q.PlayerId));
                readyCount++;
            }

            await _context.SaveChangesAsync();
        }

        // Assigns any prepared "Ready" matches to free courts, oldest-first.
        private async Task PromoteReadyMatchesToCourtsAsync(int sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active") return;

            var occupiedCourts = await _context.Matches
                .Where(m => m.SessionId == sessionId && m.Status == MatchStatus.Active && m.CourtNumber.HasValue)
                .Select(m => m.CourtNumber!.Value)
                .ToListAsync();

            var freeCourts = Enumerable.Range(1, session.NumberOfCourts)
                .Where(c => !occupiedCourts.Contains(c))
                .ToList();

            if (freeCourts.Count == 0) return;

            var readyMatches = await _context.Matches
                .Where(m => m.SessionId == sessionId && m.Status == MatchStatus.Ready)
                .OrderBy(m => m.MatchId)
                .ToListAsync();

            var readyQueue = new Queue<Match>(readyMatches);

            foreach (var court in freeCourts)
            {
                if (readyQueue.Count == 0) break;

                var next = readyQueue.Dequeue();
                next.CourtNumber = court;
                next.Status = MatchStatus.Active;
                next.StartTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // Shared by EnsureReadyMatchesAsync and CreateMatchForCourtAsync so team
        // assignment (including keeping locked pairs together) only lives in one place.
        private void AssignTeams(Match match, List<Player> selectedPlayers, Dictionary<int, int> lockedPartnerByPlayerId)
        {
            var team1 = new List<Player>();
            var team2 = new List<Player>();
            foreach (var p in selectedPlayers)
            {
                var target = team1.Count <= team2.Count ? team1 : team2;
                if (lockedPartnerByPlayerId.TryGetValue(p.PlayerId, out var partnerId)
                    && selectedPlayers.Any(sp => sp.PlayerId == partnerId))
                {
                    target = team1.Count < 2 ? team1 : team2;
                }
                target.Add(p);
            }

            foreach (var p in team1)
                _context.MatchPlayers.Add(new MatchPlayer { MatchId = match.MatchId, PlayerId = p.PlayerId, TeamNumber = 1 });
            foreach (var p in team2)
                _context.MatchPlayers.Add(new MatchPlayer { MatchId = match.MatchId, PlayerId = p.PlayerId, TeamNumber = 2 });
        }

        private List<Player> PickFourForNextCourt(List<QueueEntry> remainingQueue, Dictionary<int, int> lockedPartnerByPlayerId)
        {
            var selected = new List<Player>();
            var selectedIds = new HashSet<int>();
            var queuedPlayerIds = remainingQueue.Select(q => q.PlayerId).ToHashSet();
            var consideredPairs = new HashSet<int>();

            // Pass 1: pull in any complete locked pair first, wherever it sits in the queue.
            foreach (var entry in remainingQueue)
            {
                if (selected.Count >= 4) break;
                if (selectedIds.Contains(entry.PlayerId) || consideredPairs.Contains(entry.PlayerId)) continue;

                if (lockedPartnerByPlayerId.TryGetValue(entry.PlayerId, out var partnerId) && queuedPlayerIds.Contains(partnerId))
                {
                    consideredPairs.Add(entry.PlayerId);
                    consideredPairs.Add(partnerId);

                    if (selected.Count <= 2)
                    {
                        var partnerEntry = remainingQueue.First(q => q.PlayerId == partnerId);
                        selected.Add(entry.Player);
                        selected.Add(partnerEntry.Player);
                        selectedIds.Add(entry.PlayerId);
                        selectedIds.Add(partnerId);
                    }
                }
            }

            // Pass 2: fill any remaining slots with whoever's next in normal queue order.
            foreach (var entry in remainingQueue)
            {
                if (selected.Count >= 4) break;
                if (selectedIds.Contains(entry.PlayerId)) continue;

                selected.Add(entry.Player);
                selectedIds.Add(entry.PlayerId);
            }

            return selected;
        }

        // ---------------- Match Results ----------------

        public async Task<Match> RecordMatchResultAsync(int sessionId, int courtNumber, int team1Score, int team2Score, List<int> team1Players, List<int> team2Players)
        {
            var match = await _context.Matches
                .Include(m => m.MatchPlayers)
                .FirstOrDefaultAsync(m => m.SessionId == sessionId
                               && m.CourtNumber == courtNumber
                               && m.Status == MatchStatus.Active);

            if (match == null)
                throw new InvalidOperationException("Active match not found.");

            match.Team1Score = team1Score;
            match.Team2Score = team2Score;
            match.IsCompleted = true;
            match.Status = MatchStatus.Completed;
            match.EndTime = DateTime.UtcNow;

            _context.MatchPlayers.RemoveRange(match.MatchPlayers);

            bool team1Won = team1Score > team2Score;
            bool team2Won = team2Score > team1Score;

            foreach (var playerId in team1Players)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = playerId,
                    TeamNumber = 1,
                    IsWinner = team1Won
                });
            }

            foreach (var playerId in team2Players)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = playerId,
                    TeamNumber = 2,
                    IsWinner = team2Won
                });
            }

            await _context.SaveChangesAsync();

            await RecordPlayerMatchHistoryAsync(match.MatchId);
            await UpdatePlayerStatsAfterMatchAsync(match.MatchId);

            _leaderboardService.InvalidateOverallLeaderboardCache();

            await HandlePostMatchAsync(sessionId, team1Players.Concat(team2Players).ToList());

            return match;
        }

        public async Task HandlePostMatchAsync(int sessionId, List<int> playerIds)
        {
            foreach (var pid in playerIds)
            {
                await UpsertWaitingQueueEntryAsync(sessionId, pid);
            }

            await _context.SaveChangesAsync();

            await UpdateQueuePrioritiesAsync(sessionId);
            await AutoFillCourtsFromQueueAsync(sessionId);
        }

        public async Task RecordPlayerMatchHistoryAsync(int matchId)
        {
            var matchPlayers = await GetMatchPlayersAsync(matchId);

            var team1 = matchPlayers.Where(mp => mp.TeamNumber == 1).ToList();
            var team2 = matchPlayers.Where(mp => mp.TeamNumber == 2).ToList();

            foreach (var mp in matchPlayers)
            {
                var history = new PlayerMatchHistory
                {
                    PlayerId = mp.PlayerId,
                    MatchId = matchId,
                    PlayedAt = DateTime.UtcNow,
                    PartnerId = mp.TeamNumber == 1
                        ? team1.FirstOrDefault(p => p.PlayerId != mp.PlayerId)?.PlayerId
                        : team2.FirstOrDefault(p => p.PlayerId != mp.PlayerId)?.PlayerId
                };

                var opponents = mp.TeamNumber == 1 ? team2 : team1;
                if (opponents.Count >= 1) history.Opponent1Id = opponents[0].PlayerId;
                if (opponents.Count >= 2) history.Opponent2Id = opponents[1].PlayerId;

                _context.PlayerMatchHistories.Add(history);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdatePlayerStatsAfterMatchAsync(int matchId)
        {
            var matchPlayers = await GetMatchPlayersAsync(matchId);

            foreach (var mp in matchPlayers)
            {
                var player = await _context.Players.FindAsync(mp.PlayerId);
                if (player == null) continue;

                player.GamesPlayed += 1;

                if (mp.IsWinner == true)
                    player.TotalWins += 1;
                else
                    player.TotalLosses += 1;

                player.WinPercentage = player.GamesPlayed > 0
                    ? Math.Round((decimal)player.TotalWins * 100 / player.GamesPlayed, 2)
                    : 0;

                player.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<Match>> GetSessionMatchesAsync(int sessionId)
        {
            return await _context.Matches
                .Where(m => m.SessionId == sessionId && m.IsCompleted)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.Player)
                .OrderByDescending(m => m.EndTime)
                .ToListAsync();
        }

        public async Task<List<Match>> GetActiveMatchesAsync(int sessionId)
        {
            return await _context.Matches
                .Where(m => m.SessionId == sessionId && m.Status == MatchStatus.Active)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.Player)
                .OrderBy(m => m.StartTime)
                .ToListAsync();
        }

        // ---------------- Helpers ----------------

        private decimal CalculatePriority(QueueEntry entry)
        {
            var waiting = (decimal)(DateTime.UtcNow - entry.CheckInTime).TotalMinutes * 10m;
            var skill = entry.Player.SkillLevel * 30m;
            var winp = entry.Player.WinPercentage * 0.5m;
            return waiting + skill + winp;
        }

        private async Task<List<MatchPlayer>> GetMatchPlayersAsync(int matchId)
        {
            return await _context.MatchPlayers
                .Where(mp => mp.MatchId == matchId)
                .ToListAsync();
        }
    }
}