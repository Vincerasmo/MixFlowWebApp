using MixFlowWebApp.Constants;

namespace MixFlowWebApp.Models
{
    public class Match
    {
        public int MatchId { get; set; }
        public int SessionId { get; set; }
        public int? CourtNumber { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string MatchType { get; set; } = "Doubles";
        public string? RotationMode { get; set; }

        // See Constants.MatchStatus for the lifecycle this drives (Ready -> Active -> Completed).
        public string Status { get; set; } = MatchStatus.Ready;

        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public bool IsCompleted { get; set; } = false;

        public Session Session { get; set; } = null!;
        public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    }
}