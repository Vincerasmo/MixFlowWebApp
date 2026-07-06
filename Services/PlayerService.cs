using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.PlayerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly MixFlowDbContext _context;

        public PlayerService(MixFlowDbContext context)
        {
            _context = context;
        }

        /// Add a new player (returns entity).
        public async Task<Player> AddPlayerAsync(Player player)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        /// Get a player by ID (returns entity).
        public async Task<Player?> GetPlayerByIdAsync(int playerId)
        {
            return await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);
        }

        /// Get all players (returns entities).
        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _context.Players.ToListAsync();
        }

        /// Update player details (returns entity).
        public async Task<Player?> UpdatePlayerAsync(int playerId, Player updatedPlayer)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return null;

            // Update only non-null values
            player.FullName = updatedPlayer.FullName ?? player.FullName;
            player.SkillCategory = updatedPlayer.SkillCategory ?? player.SkillCategory;
            player.SkillLevel = updatedPlayer.SkillLevel != 0 ? updatedPlayer.SkillLevel : player.SkillLevel;
            player.DUPR = updatedPlayer.DUPR ?? player.DUPR;
            player.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return player;
        }

        /// Delete a player (hard delete).
        public async Task<bool> DeletePlayerAsync(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return false;

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
