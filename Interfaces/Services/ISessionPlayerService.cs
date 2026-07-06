using MixFlowWebApp.DTOs.SessionPlayerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ISessionPlayerService
    {
        Task<SessionPlayer?> AddPlayerToSessionAsync(int sessionId, int playerId);
        Task<SessionPlayer?> BenchPlayerAsync(int sessionId, int playerId, string reason);
        Task<SessionPlayer?> ReturnFromBenchAsync(int sessionId, int playerId);
        Task<List<SessionPlayer>> GetSessionPlayersAsync(int sessionId);
        Task<List<SessionPlayer>> GetBenchPlayersAsync(int sessionId);
        Task<bool> RemovePlayerFromSessionAsync(int sessionId, int playerId);
    }
}
