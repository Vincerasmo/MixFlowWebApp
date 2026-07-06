using MixFlowWebApp.DTOs.PlayerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IPlayerService
    {
        Task<Player> AddPlayerAsync(Player player);
        Task<Player?> GetPlayerByIdAsync(int playerId);
        Task<List<Player>> GetAllPlayersAsync();
        Task<Player?> UpdatePlayerAsync(int playerId, Player updatedPlayer);
        Task<bool> DeletePlayerAsync(int playerId);
    }
}
