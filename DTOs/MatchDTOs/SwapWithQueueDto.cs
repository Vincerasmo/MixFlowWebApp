namespace MixFlowWebApp.DTOs.MatchDTOs
{
    // Replaces a player currently in a "next up" (Ready) match with a player who's
    // waiting in the queue. PlayerOutId goes back to the queue; PlayerInId takes their spot.
    public class SwapWithQueueDto
    {
        public int PlayerOutId { get; set; }
        public int PlayerInId { get; set; }
    }
}