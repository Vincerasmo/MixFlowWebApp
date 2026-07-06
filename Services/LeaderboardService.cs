using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly MixFlowDbContext _context;
        private readonly IMemoryCache _cache;
        private const int SessionLeaderboardLimit = 30;
        private const int OverallLeaderboardLimit = 30;

        public LeaderboardService(MixFlowDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ---------------- Session Leaderboard ----------------
        public async Task<List<LeaderboardPlayerDto>> GetSessionLeaderboardAsync(int sessionId)
        {
            var players = await _context.SessionPlayers
                .Where(sp => sp.SessionId == sessionId)
                .Select(sp => new LeaderboardPlayerDto
                {
                    PlayerId = sp.PlayerId,
                    FullName = sp.Player.FullName,
                    GamesPlayed = sp.Player.GamesPlayed,
                    Wins = sp.Player.TotalWins,
                    Losses = sp.Player.TotalLosses,
                    WinPercentage = sp.Player.WinPercentage,
                    SkillLevel = sp.Player.SkillLevel
                })
                .OrderByDescending(p => p.Wins)
                .ThenByDescending(p => p.WinPercentage)
                .ThenByDescending(p => p.SkillLevel)
                .Take(SessionLeaderboardLimit)
                .ToListAsync();

            // ✅ Assign rank based on sorted order
            int rank = 1;
            foreach (var player in players)
            {
                player.Rank = rank++;
            }

            return players;
        }

        // ---------------- Overall Leaderboard ----------------
        public async Task<List<LeaderboardPlayerDto>> GetOverallLeaderboardAsync()
        {
            string cacheKey = "OverallLeaderboard";
            if (!_cache.TryGetValue(cacheKey, out List<LeaderboardPlayerDto>? players))
            {
                players = await _context.Players
                    .OrderByDescending(p => p.TotalWins)
                    .ThenByDescending(p => p.WinPercentage)
                    .ThenByDescending(p => p.SkillLevel)
                    .Take(OverallLeaderboardLimit)
                    .Select(p => new LeaderboardPlayerDto
                    {
                        PlayerId = p.PlayerId,
                        FullName = p.FullName,
                        GamesPlayed = p.GamesPlayed,
                        Wins = p.TotalWins,
                        Losses = p.TotalLosses,
                        WinPercentage = p.WinPercentage,
                        SkillLevel = p.SkillLevel
                    })
                    .ToListAsync();

                // ✅ Assign rank
                int rank = 1;
                foreach (var player in players)
                {
                    player.Rank = rank++;
                }

                _cache.Set(cacheKey, players, TimeSpan.FromMinutes(10080)); // cache for 1 week
            }
            return players!;
        }

        // ---------------- Snapshots ----------------
        public async Task SaveSessionLeaderboardSnapshotAsync(int sessionId)
        {
            var players = await GetSessionLeaderboardAsync(sessionId);

            var leaderboard = new Leaderboard
            {
                Type = "Session",
                SessionId = sessionId,
                GeneratedAt = DateTime.UtcNow,
                Entries = players.Select(p => new LeaderboardEntry
                {
                    PlayerId = p.PlayerId,
                    WinPercentage = p.WinPercentage,
                    GamesPlayed = p.GamesPlayed,
                    SkillLevel = p.SkillLevel,
                    Wins = p.Wins,
                    Losses = p.Losses,
                    Rank = p.Rank
                }).ToList()
            };

            _context.Leaderboards.Add(leaderboard);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Leaderboard>> GetSessionLeaderboardSnapshotsAsync(int sessionId)
        {
            return await _context.Leaderboards
                .Include(l => l.Entries)
                .ThenInclude(e => e.Player)
                .Where(l => l.Type == "Session" && l.SessionId == sessionId)
                .OrderByDescending(l => l.GeneratedAt)
                .ToListAsync();
        }
    }
}
