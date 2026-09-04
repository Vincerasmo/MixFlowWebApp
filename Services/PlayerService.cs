using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Constants;
using MixFlowWebApp.Data;
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

        /// Add a new player, owned by this organizer (returns entity).
        public async Task<Player> AddPlayerAsync(int organizerId, Player player)
        {
            player.OrganizerId = organizerId;

            if (!string.IsNullOrEmpty(player.SkillCategory) &&
                SkillRatingDefaults.Ratings.TryGetValue(player.SkillCategory, out var rating))
            {
                player.SkillLevel = rating; // auto-assign predefined rating
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        /// Get a player by ID, only if they belong to this organizer (returns entity).
        public async Task<Player?> GetPlayerByIdAsync(int organizerId, int playerId)
        {
            return await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.OrganizerId == organizerId);
        }

        /// Get every player belonging to this organizer (returns entities).
        public async Task<List<Player>> GetPlayersByOrganizerAsync(int organizerId)
        {
            return await _context.Players
                .Where(p => p.OrganizerId == organizerId)
                .ToListAsync();
        }

        /// Update player details, only if they belong to this organizer (returns entity).
        public async Task<Player?> UpdatePlayerAsync(int organizerId, int playerId, Player updatedPlayer)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.OrganizerId == organizerId);
            if (player == null) return null;

            // Update only non-null values
            player.FullName = updatedPlayer.FullName ?? player.FullName;

            if (!string.IsNullOrEmpty(updatedPlayer.SkillCategory) &&
                SkillRatingDefaults.Ratings.TryGetValue(updatedPlayer.SkillCategory, out var rating))
            {
                player.SkillCategory = updatedPlayer.SkillCategory;
                player.SkillLevel = rating; // auto-assign predefined rating
            }

            player.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return player;
        }

        /// Delete a player, only if they belong to this organizer (hard delete).
        public async Task<bool> DeletePlayerAsync(int organizerId, int playerId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.OrganizerId == organizerId);
            if (player == null) return false;

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}