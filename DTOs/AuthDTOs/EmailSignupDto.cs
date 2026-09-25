using System.ComponentModel.DataAnnotations;

namespace MixFlowWebApp.DTOs.AuthDTOs
{
    public class EmailSignupDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [RegularExpression(@"^[A-Za-z]+(?: [A-Za-z]+)*$",
            ErrorMessage = "Name can only contain letters and spaces — no numbers or special characters.")]
        public string FullName { get; set; } = string.Empty;

        // Deliberately NOT using [RegularExpression] here for the @gmail.com check —
        // that validation now lives explicitly inside AuthController.EmailSignup, so it
        // can never be silently skipped by a model-binding/validation-pipeline quirk and
        // always returns this app's normal { error: "..." } shape instead of ASP.NET's
        // separate ValidationProblem format.
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; } = string.Empty;
    }
}