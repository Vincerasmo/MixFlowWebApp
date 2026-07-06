namespace MixFlowWebApp.DTOs.LeaderboardDTOs
{
    public class LeaderboardDto
    {
        public List<LeaderboardPlayerDto> SessionLeaders { get; set; } = new();
        public List<LeaderboardPlayerDto> OverallLeaders { get; set; } = new();
    }
}
