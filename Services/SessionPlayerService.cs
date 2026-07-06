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

        /// Bench a player in a session.
        public async Task<SessionPlayer?> BenchPlayerAsync(int sessionId, int playerId, string reason)
        {
            var sp = await _context.SessionPlayers
                .Include(sp => sp.Player)
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null || sp.Status == SessionPlayerStatus.Benched) return null;

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

        /// Get all players in a session.
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

        /// Remove a player from a session.
        public async Task<bool> RemovePlayerFromSessionAsync(int sessionId, int playerId)
        {
            var sp = await _context.SessionPlayers
                .FirstOrDefaultAsync(sp => sp.SessionId == sessionId && sp.PlayerId == playerId);

            if (sp == null) return false;

            _context.SessionPlayers.Remove(sp);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
