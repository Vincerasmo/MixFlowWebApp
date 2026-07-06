namespace MixFlowWebApp.Models
{
    public class LeaderboardEntry
    {
        public int LeaderboardEntryId { get; set; }  // Primary key
        public int LeaderboardId { get; set; }       // FK to Leaderboard
        public int PlayerId { get; set; }            // FK to Player

        public decimal WinPercentage { get; set; }
        public int GamesPlayed { get; set; }
        public decimal SkillLevel { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Rank { get; set; }                // Position in leaderboard

        // Navigation properties
        public Leaderboard? Leaderboard { get; set; }
        public Player? Player { get; set; }
    }
}
