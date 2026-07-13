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
        private const string OverallLeaderboardCacheKey = "OverallLeaderboard";

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
        // ---------------- Overall Leaderboard (weekly, resets every Sunday) ----------------
        public async Task<List<LeaderboardPlayerDto>> GetOverallLeaderboardAsync()
        {
            var startOfWeek = GetStartOfWeekSunday(DateTime.UtcNow);
            var cacheKey = $"{OverallLeaderboardCacheKey}_{startOfWeek:yyyyMMdd}";

            if (!_cache.TryGetValue(cacheKey, out List<LeaderboardPlayerDto>? players))
            {
                var weeklyStats = await _context.MatchPlayers
                    .Where(mp => mp.Match.IsCompleted && mp.Match.EndTime != null && mp.Match.EndTime >= startOfWeek)
                    .GroupBy(mp => mp.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        GamesPlayed = g.Count(),
                        Wins = g.Count(x => x.IsWinner == true),
                        Losses = g.Count(x => x.IsWinner == false)
                    })
                    .Where(x => x.GamesPlayed > 0)
                    .ToListAsync();

                var playerIds = weeklyStats.Select(w => w.PlayerId).ToList();
                var playerInfo = await _context.Players
                    .Where(p => playerIds.Contains(p.PlayerId))
                    .ToDictionaryAsync(p => p.PlayerId, p => p);

                players = weeklyStats
                    .Where(w => playerInfo.ContainsKey(w.PlayerId))
                    .Select(w => new LeaderboardPlayerDto
                    {
                        PlayerId = w.PlayerId,
                        FullName = playerInfo[w.PlayerId].FullName,
                        GamesPlayed = w.GamesPlayed,
                        Wins = w.Wins,
                        Losses = w.Losses,
                        WinPercentage = w.GamesPlayed > 0 ? Math.Round((decimal)w.Wins * 100 / w.GamesPlayed, 2) : 0,
                        SkillLevel = playerInfo[w.PlayerId].SkillLevel
                    })
                    .OrderByDescending(p => p.Wins)
                    .ThenByDescending(p => p.WinPercentage)
                    .ThenByDescending(p => p.SkillLevel)
                    .Take(OverallLeaderboardLimit)
                    .ToList();

                int rank = 1;
                foreach (var player in players)
                {
                    player.Rank = rank++;
                }

                _cache.Set(cacheKey, players, TimeSpan.FromMinutes(10080));
            }
            return players!;
        }

        public void InvalidateOverallLeaderboardCache()
        {
            var startOfWeek = GetStartOfWeekSunday(DateTime.UtcNow);
            _cache.Remove($"{OverallLeaderboardCacheKey}_{startOfWeek:yyyyMMdd}");
        }

        private static DateTime GetStartOfWeekSunday(DateTime utcNow)
        {
            var diff = (int)utcNow.Date.DayOfWeek; // Sunday = 0
            return utcNow.Date.AddDays(-diff);
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