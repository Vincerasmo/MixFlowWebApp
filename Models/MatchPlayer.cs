namespace MixFlowWebApp.Models
{
    public class MatchPlayer
    {
        public int MatchPlayerId { get; set; }
        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        public int TeamNumber { get; set; }
        public bool? IsWinner { get; set; }

        public Match Match { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }
}
