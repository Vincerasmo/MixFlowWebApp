namespace MixFlowWebApp.Models
{
    public class PlayerMatchHistory
    {
        public int HistoryId { get; set; }
        public int PlayerId { get; set; }
        public int MatchId { get; set; }
        public int? PartnerId { get; set; }
        public int? Opponent1Id { get; set; }
        public int? Opponent2Id { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        public Player Player { get; set; } = null!;
    }
}
