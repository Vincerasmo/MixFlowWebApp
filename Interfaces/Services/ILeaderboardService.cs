using MixFlowWebApp.DTOs.LeaderboardDTOs;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ILeaderboardService
    {
        Task<List<LeaderboardPlayerDto>> GetSessionLeaderboardAsync(int sessionId);
        Task<List<LeaderboardPlayerDto>> GetOverallLeaderboardAsync(int organizerId);

        /// Clears the cached overall leaderboard so the next read picks up fresh stats.
        /// Must be called any time player win/loss stats change (i.e. after a match result
        /// is recorded) — otherwise the 1-week cache silently serves stale/empty rankings.
        void InvalidateOverallLeaderboardCache(int organizerId);
    }
}