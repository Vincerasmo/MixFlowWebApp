using System.ComponentModel.DataAnnotations;

namespace MixFlowWebApp.DTOs.PlayerDTOs
{
    public class CreatePlayerDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string SkillCategory { get; set; } = string.Empty;

        [Required]
        [Range(1.0, 6.0)]
        public decimal SkillLevel { get; set; }

        public decimal? DUPR { get; set; }
    }
}
