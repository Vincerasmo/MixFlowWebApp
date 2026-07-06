namespace MixFlowWebApp.DTOs.PlayerDTOs
{
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public decimal SkillLevel { get; set; }
        public decimal? DUPR { get; set; }
        public decimal WinPercentage { get; set; }
        public int GamesPlayed { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
    }
}
