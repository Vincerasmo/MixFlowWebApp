namespace MixFlowWebApp.DTOs.PlayerDTOs
{
    public class PlayerMatchHistoryEntryDto
    {
        public int MatchId { get; set; }
        public DateTime PlayedAt { get; set; }
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public List<string> OpponentNames { get; set; } = new();
        public bool Won { get; set; }
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
    }

    public class PlayerHistoryDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public decimal SkillLevel { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public decimal WinPercentage { get; set; }
        public int SessionsPlayed { get; set; }
        // Most recent first — same convention as everywhere else results are listed.
        public List<PlayerMatchHistoryEntryDto> Matches { get; set; } = new();
    }
}