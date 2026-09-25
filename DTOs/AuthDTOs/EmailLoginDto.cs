using System.ComponentModel.DataAnnotations;

namespace MixFlowWebApp.DTOs.AuthDTOs
{
    public class EmailLoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@gmail\.com$",
            ErrorMessage = "Email must be a valid @gmail.com address.")]
        public string Email { get; set; } = string.Empty;
    }
}