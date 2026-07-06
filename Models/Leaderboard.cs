namespace MixFlowWebApp.Models
{
    public class Leaderboard
    {
        public int LeaderboardId { get; set; }   // Primary key
        public string Type { get; set; } = "Session"; // "Session" or "Overall"
        public int? SessionId { get; set; }      // Null if overall leaderboard
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation property: players included in this leaderboard
        public List<LeaderboardEntry> Entries { get; set; } = new();
    }
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
