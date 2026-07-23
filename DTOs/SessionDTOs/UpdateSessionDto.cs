namespace MixFlowWebApp.DTOs.SessionDTOs
{
    public class UpdateSessionDto
    {
        public string? SessionName { get; set; }
        public string? Status { get; set; }
        public DateTime? SessionDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? NumberOfCourts { get; set; }
    }
}