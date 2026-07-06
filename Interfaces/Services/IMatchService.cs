using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IMatchService
    {
        // Queue management
        Task<QueueEntry?> EnqueuePlayerAsync(int sessionId, int playerId);
        Task<List<QueueEntry>> GetCurrentQueueAsync(int sessionId);
        Task UpdateQueuePrioritiesAsync(int sessionId);
        Task<bool> RemoveFromQueueAsync(int sessionId, int playerId);

        // Match creation
        Task<Match?> CreateNextSmartMixAsync(int sessionId, List<(int PlayerId, int PartnerId)> requestedPairs);
        Task AutoFillCourtsFromQueueAsync(int sessionId);

        // Match results
        Task<Match> RecordMatchResultAsync(int sessionId, int courtNumber, int team1Score, int team2Score, List<int> team1Players, List<int> team2Players);
        Task HandlePostMatchAsync(int sessionId, List<int> playerIds);

        // Player history & stats
        Task RecordPlayerMatchHistoryAsync(int matchId);
        Task UpdatePlayerStatsAfterMatchAsync(int matchId);

        // Queries
        Task<List<Match>> GetSessionMatchesAsync(int sessionId);   // Completed matches
        Task<List<Match>> GetActiveMatchesAsync(int sessionId);    // Ongoing matches
    }
}
