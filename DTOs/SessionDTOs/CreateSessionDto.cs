using System.ComponentModel.DataAnnotations;

namespace MixFlowWebApp.DTOs.SessionDTOs
{
    public class CreateSessionDto
    {
        [Required]
        public string SessionName { get; set; } = string.Empty;

        [Required]
        public DateTime SessionDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int NumberOfCourts { get; set; }
    }
}
