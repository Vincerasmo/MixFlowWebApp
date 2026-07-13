using MixFlowWebApp.DTOs.SessionDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ISessionService
    {
        Task<Session> CreateSessionAsync(int organizerId, Session session);
        Task<Session?> GetSessionByIdAsync(int sessionId);
        Task<List<Session>> GetOrganizerSessionsAsync(int organizerId);
        Task<Session?> GetActiveSessionByOrganizerAsync(int organizerId);
        Task<Session?> UpdateSessionAsync(int sessionId, Session updatedSession);
        Task<bool> EndSessionAsync(int sessionId);
    }
}