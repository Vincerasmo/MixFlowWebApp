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

        // The PlayerId of the partner this player is locked with in this session, if any.
        // Symmetric: when A locks with B, both rows get each other's PlayerId here.
        // Not FK-constrained (deliberately) since it points to a PlayerId, not a SessionPlayerId,
        // and only makes sense scoped to this same SessionId.
        public int? LockedPartnerId { get; set; }

        public Session Session { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }
}