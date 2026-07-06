using MixFlowWebApp.DTOs.OrganizerDTOs;

namespace MixFlowWebApp.DTOs.AuthDTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public OrganizerDto Organizer { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
    }
}
