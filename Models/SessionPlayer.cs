namespace MixFlowWebApp.Models
{
    public class SessionPlayer
    {
        public int SessionPlayerId { get; set; }
        public int SessionId { get; set; }
        public int PlayerId { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string Status { get; set; } = "Registered";
        public string? BenchReason { get; set; }
        public DateTime? BenchedAt { get; set; }

        public Session Session { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }
}
