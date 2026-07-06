namespace MixFlowWebApp.DTOs.MatchDTOs
{
    public class MatchPlayerDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TeamNumber { get; set; }
        public bool? IsWinner { get; set; }
    }
}
