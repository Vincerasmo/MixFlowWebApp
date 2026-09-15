using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;

namespace MixFlowWebApp.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly MixFlowDbContext _context;
        private const int SessionLeaderboardLimit = 30;

        public LeaderboardService(MixFlowDbContext context)
        {
            _context = context;
        }

        // ---------------- Session Leaderboard ----------------
        public async Task<List<LeaderboardPlayerDto>> GetSessionLeaderboardAsync(int sessionId)
        {
            // Scoped to matches actually played in THIS session, not the player's career
            // totals. Pulled as flat per-match rows (not a SQL-side aggregate) because
            // streak needs each player's results in chronological order, which a GroupBy
            // aggregate can't express.
            var matchResults = await _context.MatchPlayers
                .Where(mp => mp.Match.SessionId == sessionId && mp.Match.IsCompleted)
                .Select(mp => new { mp.PlayerId, mp.IsWinner, mp.Match.EndTime })
                .ToListAsync();

            var playerIds = matchResults.Select(r => r.PlayerId).Distinct().ToList();
            var playerInfo = await _context.Players
                .Where(p => playerIds.Contains(p.PlayerId))
                .ToDictionaryAsync(p => p.PlayerId, p => p);

            var players = matchResults
                .GroupBy(r => r.PlayerId)
                .Where(g => playerInfo.ContainsKey(g.Key))
                .Select(g =>
                {
                    var gamesPlayed = g.Count();
                    var wins = g.Count(x => x.IsWinner == true);
                    var losses = g.Count(x => x.IsWinner == false);

                    return new LeaderboardPlayerDto
                    {
                        PlayerId = g.Key,
                        FullName = playerInfo[g.Key].FullName,
                        GamesPlayed = gamesPlayed,
                        Wins = wins,
                        Losses = losses,
                        WinPercentage = gamesPlayed > 0 ? Math.Round((decimal)wins * 100 / gamesPlayed, 2) : 0,
                        SkillLevel = playerInfo[g.Key].SkillLevel,
                        Streak = ComputeStreak(g.Select(x => (x.EndTime, x.IsWinner)))
                    };
                })
                .OrderByDescending(p => p.Wins)
                .ThenByDescending(p => p.WinPercentage)
                .ThenByDescending(p => p.SkillLevel)
                .Take(SessionLeaderboardLimit)
                .ToList();

            int rank = 1;
            foreach (var player in players)
            {
                player.Rank = rank++;
            }

            return players;
        }

        // Positive = consecutive wins, negative = consecutive losses, 0 = no completed
        // matches (IsWinner is only null for a match that hasn't been scored yet, which
        // shouldn't reach here since callers already filter to Match.IsCompleted).
        private static int ComputeStreak(IEnumerable<(DateTime? EndTime, bool? IsWinner)> results)
        {
            var ordered = results
                .Where(r => r.IsWinner.HasValue)
                .OrderByDescending(r => r.EndTime)
                .Select(r => r.IsWinner!.Value)
                .ToList();

            if (ordered.Count == 0) return 0;

            var mostRecentWasWin = ordered[0];
            var streak = 0;
            foreach (var isWinner in ordered)
            {
                if (isWinner != mostRecentWasWin) break;
                streak++;
            }

            return mostRecentWasWin ? streak : -streak;
        }
    }
}