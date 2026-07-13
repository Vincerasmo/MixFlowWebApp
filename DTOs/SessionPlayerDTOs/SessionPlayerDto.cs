namespace MixFlowWebApp.DTOs.SessionPlayerDTOs
{
    public class SessionPlayerDto
    {
        public int SessionPlayerId { get; set; }
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public decimal SkillLevel { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BenchReason { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? BenchedAt { get; set; }

        // How many matches this player has played within THIS session (not lifetime).
        public int GamesPlayedInSession { get; set; }

        // Lock-pair info, if this player has locked in with a partner for this session.
        public int? LockedPartnerId { get; set; }
        public string? LockedPartnerName { get; set; }
    }
}