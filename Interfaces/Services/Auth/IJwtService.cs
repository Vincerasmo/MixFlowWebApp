using MixFlowWebApp.DTOs.OrganizerDTOs;
using MixFlowWebApp.Models;
using System.Security.Claims;

namespace MixFlowWebApp.Interfaces.Services.Auth
{
    public interface IJwtService
    {
        string GenerateToken(Organizer organizer);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
