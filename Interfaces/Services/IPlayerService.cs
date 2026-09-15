using MixFlowWebApp.DTOs.PlayerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IPlayerService
    {
        Task<Player> AddPlayerAsync(int organizerId, Player player);
        Task<Player?> GetPlayerByIdAsync(int organizerId, int playerId);
        Task<List<Player>> GetPlayersByOrganizerAsync(int organizerId);
        Task<Player?> UpdatePlayerAsync(int organizerId, int playerId, Player updatedPlayer);
        Task<bool> DeletePlayerAsync(int organizerId, int playerId);
        Task<PlayerHistoryDto?> GetPlayerHistoryAsync(int organizerId, int playerId);
    }
}