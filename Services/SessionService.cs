using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.SessionDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public static class SessionStatus
    {
        public const string Active = "Active";
        public const string Completed = "Completed";
    }

    public class SessionService : ISessionService
    {
        private readonly MixFlowDbContext _context;

        public SessionService(MixFlowDbContext context)
        {
            _context = context;
        }

        /// Create a new session for an organizer (returns entity).
        public async Task<Session> CreateSessionAsync(int organizerId, Session session)
        {
            session.OrganizerId = organizerId;
            session.Status = SessionStatus.Active;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        /// Get a session by ID, including players (returns entity).
        public async Task<Session?> GetSessionByIdAsync(int sessionId)
        {
            return await _context.Sessions
                .Include(s => s.SessionPlayers)
                    .ThenInclude(sp => sp.Player)
                .Include(s => s.Matches)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<List<Session>> GetOrganizerSessionsAsync(int organizerId)
        {
            return await _context.Sessions
                .Where(s => s.OrganizerId == organizerId)
                .Include(s => s.Matches)
                .OrderByDescending(s => s.SessionDate)
                .ToListAsync();
        }

        /// Get the organizer's current active session, if one exists.
        /// If an organizer somehow has more than one Active session, the most recently
        /// dated one wins — but normal flow (CreateSession sets Active, EndSession sets
        /// Completed) should only ever leave at most one Active session per organizer.
        public async Task<Session?> GetActiveSessionByOrganizerAsync(int organizerId)
        {
            return await _context.Sessions
                .Where(s => s.OrganizerId == organizerId && s.Status == SessionStatus.Active)
                .OrderByDescending(s => s.SessionDate)
                .FirstOrDefaultAsync();
        }

        /// Update session details (returns entity).
        public async Task<Session?> UpdateSessionAsync(int sessionId, Session updatedSession)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return null;

            // Update only non-null or non-default values
            session.SessionName = updatedSession.SessionName ?? session.SessionName;
            session.SessionDate = updatedSession.SessionDate != default ? updatedSession.SessionDate : session.SessionDate;
            session.StartTime = updatedSession.StartTime != default ? updatedSession.StartTime : session.StartTime;
            session.EndTime = updatedSession.EndTime != default ? updatedSession.EndTime : session.EndTime;
            session.NumberOfCourts = updatedSession.NumberOfCourts != 0 ? updatedSession.NumberOfCourts : session.NumberOfCourts;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return session;
        }

        /// End a session (mark as completed).
        public async Task<bool> EndSessionAsync(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = SessionStatus.Completed;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}