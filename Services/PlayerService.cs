using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Constants;
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

        public async Task<PlayerHistoryDto?> GetPlayerHistoryAsync(int organizerId, int playerId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.OrganizerId == organizerId);
            if (player == null) return null;

            var history = await _context.PlayerMatchHistories
                .Where(h => h.PlayerId == playerId)
                .OrderByDescending(h => h.PlayedAt)
                .ToListAsync();

            var matchIds = history.Select(h => h.MatchId).ToList();
            var matches = await _context.Matches
                .Where(m => matchIds.Contains(m.MatchId))
                .Include(m => m.MatchPlayers)
                .Include(m => m.Session)
                .ToDictionaryAsync(m => m.MatchId, m => m);

            var relatedPlayerIds = history
                .SelectMany(h => new[] { h.PartnerId, h.Opponent1Id, h.Opponent2Id })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var relatedPlayerNames = await _context.Players
                .Where(p => relatedPlayerIds.Contains(p.PlayerId))
                .ToDictionaryAsync(p => p.PlayerId, p => p.FullName);

            var entries = new List<PlayerMatchHistoryEntryDto>();
            foreach (var h in history)
            {
                if (!matches.TryGetValue(h.MatchId, out var match)) continue;

                var self = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == playerId);
                if (self == null) continue;

                var teamScore = self.TeamNumber == 1 ? match.Team1Score : match.Team2Score;
                var opponentScore = self.TeamNumber == 1 ? match.Team2Score : match.Team1Score;

                entries.Add(new PlayerMatchHistoryEntryDto
                {
                    MatchId = match.MatchId,
                    PlayedAt = h.PlayedAt,
                    SessionId = match.SessionId,
                    SessionName = match.Session.SessionName,
                    PartnerName = h.PartnerId.HasValue && relatedPlayerNames.TryGetValue(h.PartnerId.Value, out var pn) ? pn : "—",
                    OpponentNames = new[] { h.Opponent1Id, h.Opponent2Id }
                        .Where(id => id.HasValue)
                        .Select(id => relatedPlayerNames.TryGetValue(id!.Value, out var on) ? on : "—")
                        .ToList(),
                    Won = self.IsWinner == true,
                    TeamScore = teamScore ?? 0,
                    OpponentScore = opponentScore ?? 0
                });
            }

            return new PlayerHistoryDto
            {
                PlayerId = player.PlayerId,
                FullName = player.FullName,
                SkillCategory = player.SkillCategory,
                SkillLevel = player.SkillLevel,
                TotalWins = player.TotalWins,
                TotalLosses = player.TotalLosses,
                WinPercentage = player.WinPercentage,
                SessionsPlayed = entries.Select(e => e.SessionId).Distinct().Count(),
                Matches = entries
            };
        }
    }
}