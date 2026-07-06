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
}
