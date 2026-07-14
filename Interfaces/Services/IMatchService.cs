using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IMatchService
    {
        // ---------------- Queue ----------------
        Task<QueueEntry?> EnqueuePlayerAsync(int sessionId, int playerId);
        Task<List<QueueEntry>> GetCurrentQueueAsync(int sessionId);
        Task UpdateQueuePrioritiesAsync(int sessionId);
        Task<bool> RemoveFromQueueAsync(int sessionId, int playerId);

        // ---------------- Match Creation (randomized only) ----------------

        // Promotes any prepared "next up" matches onto free courts and tops the
        // next-up pool back up from the queue. This is the single entry point that
        // keeps courts and the next-up queue full — called after every match result
        // and by the manual "Auto Match" button.
        Task AutoFillCourtsFromQueueAsync(int sessionId);

        // Fills exactly one specific court — promotes the next "Ready" match if one is
        // prepared, otherwise builds a fresh randomized match directly for that court.
        Task<Match?> CreateMatchForCourtAsync(int sessionId, int courtNumber);

        // ---------------- Next Up (Ready) matches ----------------

        // The prepared, not-yet-started matches waiting for a free court (target: 2).
        Task<List<Match>> GetNextUpMatchesAsync(int sessionId);

        // Swap two players' team assignments within the same next-up match.
        Task<Match> SwapMatchTeamsAsync(int sessionId, int matchId, int playerAId, int playerBId);

        // Swap a player out of a next-up match for a player currently waiting in the queue.
        Task<Match> SwapMatchWithQueueAsync(int sessionId, int matchId, int playerOutId, int playerInId);

        // ---------------- Match Results ----------------
        Task<Match> RecordMatchResultAsync(int sessionId, int courtNumber, int team1Score, int team2Score, List<int> team1Players, List<int> team2Players);
        Task HandlePostMatchAsync(int sessionId, List<int> playerIds);

        Task RecordPlayerMatchHistoryAsync(int matchId);
        Task UpdatePlayerStatsAfterMatchAsync(int matchId);

        Task<List<Match>> GetSessionMatchesAsync(int sessionId);
        Task<List<Match>> GetActiveMatchesAsync(int sessionId);
    }
}