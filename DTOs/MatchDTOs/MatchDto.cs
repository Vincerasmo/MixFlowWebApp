namespace MixFlowWebApp.DTOs.MatchDTOs
{
    public class MatchDto
    {
        public int MatchId { get; set; }
        public int SessionId { get; set; }
        public int? CourtNumber { get; set; }
        public string RotationMode { get; set; } = string.Empty;
        public List<MatchPlayerDto> Team1 { get; set; } = new();
        public List<MatchPlayerDto> Team2 { get; set; } = new();
        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public bool IsCompleted { get; set; }
    }
}
