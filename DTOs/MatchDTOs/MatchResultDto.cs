namespace MixFlowWebApp.DTOs.MatchDTOs
{
    // Record Match Result
    public class MatchResultDto
    {
        public int CourtNumber { get; set; }
        public List<int> Team1PlayerIds { get; set; } = new();
        public List<int> Team2PlayerIds { get; set; } = new();
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
    }
}
