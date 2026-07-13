using MixFlowWebApp.DTOs.SessionPlayerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Interfaces.Services
{
    public interface ISessionPlayerService
    {
        Task<SessionPlayer?> AddPlayerToSessionAsync(int sessionId, int playerId);
        Task<SessionPlayer?> BenchPlayerAsync(int sessionId, int playerId, string reason);
        Task<SessionPlayer?> ReturnFromBenchAsync(int sessionId, int playerId);
        Task<List<SessionPlayer>> GetSessionPlayersAsync(int sessionId);
        Task<List<SessionPlayer>> GetBenchPlayersAsync(int sessionId);
        Task<bool> RemovePlayerFromSessionAsync(int sessionId, int playerId);

        /// Session players enriched with games-played-in-this-session and lock-pair info.
        /// Use this for any endpoint the frontend renders a player management table from.
        Task<List<SessionPlayerDto>> GetSessionPlayersWithStatsAsync(int sessionId);

        /// Lock two players in this session as a fixed pair. Symmetric — both players'
        /// records get updated. Fails (returns false) if either player isn't checked into
        /// this session, or either is already locked with someone else.
        Task<bool> LockPairAsync(int sessionId, int playerId, int partnerId);

        /// Unlock a player from their current pair (also clears the partner's side).
        Task<bool> UnlockPairAsync(int sessionId, int playerId);
    }
}   