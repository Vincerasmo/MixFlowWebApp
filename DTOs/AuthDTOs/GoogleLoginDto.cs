namespace MixFlowWebApp.DTOs.AuthDTOs
{
    public class GoogleLoginDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
    }
}
