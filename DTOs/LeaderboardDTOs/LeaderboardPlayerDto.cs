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

        // Consecutive wins (positive) or consecutive losses (negative), counting back
        // from the player's most recent completed match. 0 if they haven't played.
        public int Streak { get; set; }
    }
}