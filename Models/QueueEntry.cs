namespace MixFlowWebApp.Models
{
    public class QueueEntry
    {
        public int QueueId { get; set; }
        public int SessionId { get; set; }
        public int PlayerId { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public decimal? PriorityScore { get; set; }
        public string Status { get; set; } = "Waiting";
        public int? Position { get; set; }

        public Session Session { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }
}
