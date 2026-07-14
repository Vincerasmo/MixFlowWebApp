namespace MixFlowWebApp.DTOs.MatchDTOs
{
    // Swaps two players' team assignments within the same "next up" (Ready) match —
    // e.g. moving PlayerAId from Team 1 to Team 2 while PlayerBId moves the other way.
    public class SwapMatchTeamsDto
    {
        public int PlayerAId { get; set; }
        public int PlayerBId { get; set; }
    }
}