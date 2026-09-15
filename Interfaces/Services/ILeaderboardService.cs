using MixFlowWebApp.DTOs.LeaderboardDTOs;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ILeaderboardService
    {
        Task<List<LeaderboardPlayerDto>> GetSessionLeaderboardAsync(int sessionId);
    }
}