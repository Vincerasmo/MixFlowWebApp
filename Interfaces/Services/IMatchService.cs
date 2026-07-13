using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface IMatchService
    {
        Task<QueueEntry?> EnqueuePlayerAsync(int sessionId, int playerId);
        Task<List<QueueEntry>> GetCurrentQueueAsync(int sessionId);
        Task UpdateQueuePrioritiesAsync(int sessionId);
        Task<bool> RemoveFromQueueAsync(int sessionId, int playerId);

        Task<Match?> CreateNextSmartMixAsync(int sessionId, List<(int PlayerId, int PartnerId)> requestedPairs);
        Task AutoFillCourtsFromQueueAsync(int sessionId);

        // Auto Mix, targeted at one specific court — randomizes from the queue.
        Task<Match?> CreateMatchForCourtAsync(int sessionId, int courtNumber);

        // Smart Mix, targeted at one specific court — organizer picks the exact 4 players.
        Task<Match> CreateManualMatchForCourtAsync(int sessionId, int courtNumber, List<SmartMixPairDto> pairs);

        Task<Match> RecordMatchResultAsync(int sessionId, int courtNumber, int team1Score, int team2Score, List<int> team1Players, List<int> team2Players);
        Task HandlePostMatchAsync(int sessionId, List<int> playerIds);

        Task RecordPlayerMatchHistoryAsync(int matchId);
        Task UpdatePlayerStatsAfterMatchAsync(int matchId);

        Task<List<Match>> GetSessionMatchesAsync(int sessionId);
        Task<List<Match>> GetActiveMatchesAsync(int sessionId);
    }
}