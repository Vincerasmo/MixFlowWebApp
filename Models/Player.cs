namespace MixFlowWebApp.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public decimal SkillLevel { get; set; }
        public decimal WinPercentage { get; set; } = 50.00m;
        public int GamesPlayed { get; set; } = 0;
        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<SessionPlayer> SessionPlayers { get; set; } = new List<SessionPlayer>();
        public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public ICollection<PlayerMatchHistory> MatchHistory { get; set; } = new List<PlayerMatchHistory>();
    }
}
