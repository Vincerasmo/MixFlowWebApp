namespace MixFlowWebApp.DTOs.SessionDTOs
{
    public class SessionDto
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int NumberOfCourts { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
