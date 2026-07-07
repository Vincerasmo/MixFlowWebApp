using System.ComponentModel.DataAnnotations;

namespace MixFlowWebApp.DTOs.PlayerDTOs
{
    public class CreatePlayerDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string SkillCategory { get; set; } = string.Empty;
    }
}
