using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.OrganizerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public class OrganizerService : IOrganizerService
    {
        private readonly MixFlowDbContext _context;

        public OrganizerService(MixFlowDbContext context)
        {
            _context = context;
        }

        /// Get an organizer by UserId (returns entity).
        public async Task<Organizer?> GetOrganizerByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.Organizers
                .FirstOrDefaultAsync(o => o.UserId == userId);
        }

        /// Get an organizer by UserId, or create one if not found (returns entity).
        public async Task<Organizer> GetOrCreateOrganizerAsync(string userId, string fullName, string email)
        {
            var organizer = await _context.Organizers
                .FirstOrDefaultAsync(o => o.UserId == userId || o.Email == email);

            if (organizer == null)
            {
                organizer = new Organizer
                {
                    UserId = userId,
                    FullName = fullName,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Organizers.Add(organizer);
                await _context.SaveChangesAsync();
            }

            return organizer;
        }

        /// Create a new organizer explicitly (returns entity).
        public async Task<Organizer?> CreateOrganizerAsync(string userId, string fullName, string email)
        {
            // Prevent duplicates
            var existing = await _context.Organizers
                .FirstOrDefaultAsync(o => o.UserId == userId || o.Email == email);

            if (existing != null) return null;

            var organizer = new Organizer
            {
                UserId = userId,
                FullName = fullName,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Organizers.Add(organizer);
            await _context.SaveChangesAsync();

            return organizer;
        }
    }
}
