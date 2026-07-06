using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ILeaderboardService
    {
        Task<List<LeaderboardPlayerDto>> GetSessionLeaderboardAsync(int sessionId);
        Task<List<LeaderboardPlayerDto>> GetOverallLeaderboardAsync();
        Task SaveSessionLeaderboardSnapshotAsync(int sessionId);
        Task<List<Leaderboard>> GetSessionLeaderboardSnapshotsAsync(int sessionId);
    }
}
