namespace MixFlowWebApp.DTOs.QueueEntryDTOs
{
    public class QueueEntryDto
    {
        public int QueueId { get; set; }
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public decimal SkillLevel { get; set; }
        public decimal? PriorityScore { get; set; }
        public int? Position { get; set; }
        public DateTime CheckInTime { get; set; }
    }
}
