using MixFlowWebApp.DTOs.OrganizerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IOrganizerService
    {
        Task<Organizer?> GetOrganizerByUserIdAsync(string userId);

        Task<Organizer?> GetOrganizerForLoginAsync(string userId, string email);

        Task<Organizer?> CreateOrganizerAsync(string userId, string fullName, string email);
    }
}
