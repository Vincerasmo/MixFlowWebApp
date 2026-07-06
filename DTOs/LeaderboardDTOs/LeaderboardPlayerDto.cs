namespace MixFlowWebApp.DTOs.LeaderboardDTOs
{
    public class LeaderboardPlayerDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public decimal WinPercentage { get; set; }
        public int GamesPlayed { get; set; }
        public decimal SkillLevel { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Rank { get; set; }
    }
}
