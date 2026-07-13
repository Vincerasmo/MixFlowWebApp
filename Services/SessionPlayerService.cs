using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        public SessionPlayerService(MixFlowDbContext context)
        {
            _context = context;
        }

        /// Add a player to a session (check-in).
        public async Task<SessionPlayer?> AddPlayerToSessionAsync(int sessionId, int playerId)
        {
            var existing = await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (existing != null) return existing;

            var sessionPlayer = new SessionPlayer
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Status = SessionPlayerStatus.CheckedIn,
                CheckInTime = DateTime.UtcNow,
            };

            _context.SessionPlayers.Add(sessionPlayer);
            await _context.SaveChangesAsync();

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

        /// Get all players in a session (raw entities).
        public async Task<List<SessionPlayer>> GetSessionPlayersAsync(int sessionId)
        {
            return await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId)
                .Include(sp => sp.Player)
                .ToListAsync();
        }

        /// Get all benched players in a session.
        public async Task<List<SessionPlayer>> GetBenchPlayersAsync(int sessionId)
        {
            return await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId && sp.Status == SessionPlayerStatus.Benched)
                .Include(sp => sp.Player)
                .ToListAsync();
        }

        /// Remove a player from a session. Also breaks their lock pair, if any, so their
        /// former partner isn't left pointing at a player no longer in this session.
        public async Task<bool> RemovePlayerFromSessionAsync(int sessionId, int playerId)
        {
            var sp = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null) return false;

            if (sp.LockedPartnerId.HasValue)
            {
                await UnlockPairAsync(sessionId, playerId);
            }

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

        /// Lock two players together as a fixed pair for this session.
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
    }
}