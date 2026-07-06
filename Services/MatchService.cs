using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Services
{
    public class MatchService : IMatchService
    {
        private readonly MixFlowDbContext _context;

        public MatchService(MixFlowDbContext context)
        {
            _context = context;
        }

        // ---------------- Queue Management ----------------

        public async Task<QueueEntry?> EnqueuePlayerAsync(int sessionId, int playerId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active")
                throw new InvalidOperationException("Session not found or not active.");

            var sp = await _context.SessionPlayers
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.PlayerId == playerId && x.Status != "Benched");
            if (sp == null)
                throw new InvalidOperationException("Player is not checked into this session or is benched.");

            var existing = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerId && q.Status == "Waiting");

            if (existing != null) return existing;

            var entry = new QueueEntry
            {
                SessionId = sessionId,
                PlayerId = playerId,
                Status = "Waiting",
                CheckInTime = DateTime.UtcNow
            };

            _context.QueueEntries.Add(entry);
            return entry;
        }

        public async Task<List<QueueEntry>> GetCurrentQueueAsync(int sessionId)
        {
            return await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderBy(q => q.Position)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();
        }

        public async Task UpdateQueuePrioritiesAsync(int sessionId)
        {
            var waiting = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .ToListAsync();

            foreach (var q in waiting)
            {
                q.PriorityScore = CalculatePriority(q);
            }

            var ordered = waiting
                .OrderByDescending(q => q.PriorityScore ?? 0m)
                .ThenBy(q => q.CheckInTime)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Position = i + 1;
            }
        }

        public async Task<bool> RemoveFromQueueAsync(int sessionId, int playerId)
        {
            var entry = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == playerId && q.Status == "Waiting");

            if (entry == null) return false;

            entry.Status = "Removed";
            entry.Position = null;

            return true;
        }

        // ---------------- Match Creation ----------------

        public async Task<Match?> CreateNextSmartMixAsync(int sessionId, List<(int PlayerId, int PartnerId)> requestedPairs)
        {
            var match = await CreateSmartMixAsync(sessionId, requestedPairs);
            if (match == null)
                throw new InvalidOperationException("Not enough players in queue (need at least 4)");

            return match;
        }

        public async Task AutoFillCourtsFromQueueAsync(int sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.Status != "Active") return;

            var active = await _context.Matches
                .CountAsync(m => m.SessionId == sessionId && !m.IsCompleted);

            var freeCourts = session.NumberOfCourts - active;
            if (freeCourts <= 0) return;

            // ✅ Use new method
            var matches = await CreateMatchesForAllCourtsAsync(sessionId, freeCourts);
        }


        // ---------------- Match Results ----------------

        public async Task<Match> RecordMatchResultAsync(int sessionId, int courtNumber, int team1Score, int team2Score, List<int> team1Players, List<int> team2Players)
        {
            var match = await _context.Matches
                .Include(m => m.MatchPlayers)
                .FirstOrDefaultAsync(m => m.SessionId == sessionId
                               && m.CourtNumber == courtNumber
                               && !m.IsCompleted);

            if (match == null)
                throw new InvalidOperationException("Active match not found.");
            if (match.IsCompleted)
                throw new InvalidOperationException("Match already completed.");

            match.Team1Score = team1Score;
            match.Team2Score = team2Score;
            match.IsCompleted = true;
            match.EndTime = DateTime.UtcNow;

            _context.MatchPlayers.RemoveRange(match.MatchPlayers);

            // ✅ Guarantee one team wins (no ties allowed)
            bool team1Won = team1Score > team2Score;
            bool team2Won = team2Score > team1Score;

            foreach (var playerId in team1Players)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = playerId,
                    TeamNumber = 1,
                    IsWinner = team1Won
                });
            }

            foreach (var playerId in team2Players)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = playerId,
                    TeamNumber = 2,
                    IsWinner = team2Won
                });
            }

            await _context.SaveChangesAsync();

            await RecordPlayerMatchHistoryAsync(match.MatchId);
            await UpdatePlayerStatsAfterMatchAsync(match.MatchId);

            // ✅ Return players back to queue
            await HandlePostMatchAsync(sessionId, team1Players.Concat(team2Players).ToList());

            return match;
        }

        public async Task HandlePostMatchAsync(int sessionId, List<int> playerIds)
        {
            foreach (var pid in playerIds)
            {
                var existing = await _context.QueueEntries
                    .FirstOrDefaultAsync(q => q.SessionId == sessionId && q.PlayerId == pid);

                if (existing == null)
                {
                    _context.QueueEntries.Add(new QueueEntry
                    {
                        SessionId = sessionId,
                        PlayerId = pid,
                        Status = "Waiting",
                        CheckInTime = DateTime.UtcNow
                    });
                }
                else
                {
                    // ✅ Reset back to Waiting if they were InMatch
                    existing.Status = "Waiting";
                    existing.CheckInTime = DateTime.UtcNow;
                    existing.Position = null;
                }
            }

            await UpdateQueuePrioritiesAsync(sessionId);
            await AutoFillCourtsFromQueueAsync(sessionId);
        }

        public async Task RecordPlayerMatchHistoryAsync(int matchId)
        {
            var matchPlayers = await GetMatchPlayersAsync(matchId);

            var team1 = matchPlayers.Where(mp => mp.TeamNumber == 1).ToList();
            var team2 = matchPlayers.Where(mp => mp.TeamNumber == 2).ToList();

            foreach (var mp in matchPlayers)
            {
                var history = new PlayerMatchHistory
                {
                    PlayerId = mp.PlayerId,
                    MatchId = matchId,
                    PlayedAt = DateTime.UtcNow,
                    PartnerId = mp.TeamNumber == 1
                        ? team1.FirstOrDefault(p => p.PlayerId != mp.PlayerId)?.PlayerId
                        : team2.FirstOrDefault(p => p.PlayerId != mp.PlayerId)?.PlayerId
                };

                var opponents = mp.TeamNumber == 1 ? team2 : team1;
                if (opponents.Count >= 1) history.Opponent1Id = opponents[0].PlayerId;
                if (opponents.Count >= 2) history.Opponent2Id = opponents[1].PlayerId;

                _context.PlayerMatchHistories.Add(history);
            }

            await _context.SaveChangesAsync();   // ✅ persist histories
        }

        public async Task UpdatePlayerStatsAfterMatchAsync(int matchId)
        {
            var matchPlayers = await GetMatchPlayersAsync(matchId);

            foreach (var mp in matchPlayers)
            {
                var player = await _context.Players.FindAsync(mp.PlayerId);
                if (player == null) continue;

                player.GamesPlayed += 1;

                if (mp.IsWinner == true)
                    player.TotalWins += 1;
                else
                    player.TotalLosses += 1;   // ✅ increment losses

                player.WinPercentage = player.GamesPlayed > 0
                    ? Math.Round((decimal)player.TotalWins * 100 / player.GamesPlayed, 2)
                    : 0;

                player.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<Match>> GetSessionMatchesAsync(int sessionId)
        {
            return await _context.Matches
                .Where(m => m.SessionId == sessionId && m.IsCompleted)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.Player)
                .OrderByDescending(m => m.EndTime)
                .ToListAsync();
        }

        public async Task<List<Match>> GetActiveMatchesAsync(int sessionId)
        {
            return await _context.Matches
                .Where(m => m.SessionId == sessionId && !m.IsCompleted)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.Player)
                .OrderBy(m => m.StartTime)
                .ToListAsync();
        }

        // ---------------- Helpers ----------------

        private async Task<int> GetNextCourtNumberAsync(int sessionId)
        {
            var existingCourts = await _context.Matches
                .Where(m => m.SessionId == sessionId && !m.IsCompleted && m.CourtNumber.HasValue)
                .Select(m => m.CourtNumber!.Value)
                .ToListAsync();

            int court = 1;
            while (existingCourts.Contains(court)) court++;
            return court;
        }

        private async Task<Match?> CreateSmartMixAsync(int sessionId, List<(int PlayerId, int PartnerId)> requestedPairs)
        {
            await UpdateQueuePrioritiesAsync(sessionId);

            var queueEntries = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderByDescending(q => q.PriorityScore)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();

            if (queueEntries.Count < 4) return null;

            var session = await _context.Sessions.FindAsync(sessionId);
            var nextCourt = await GetNextCourtNumberAsync(sessionId);

            var match = new Match
            {
                SessionId = sessionId,
                CourtNumber = nextCourt,
                RotationMode = "SmartMix",
                StartTime = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            // ✅ Honor requested pairs first
            foreach (var pair in requestedPairs)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = pair.PlayerId,
                    TeamNumber = 1
                });
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = pair.PartnerId,
                    TeamNumber = 1
                });
            }

            // ✅ Fill remaining slots with random players
            var usedIds = requestedPairs.SelectMany(p => new[] { p.PlayerId, p.PartnerId }).ToHashSet();
            var remainingPlayers = queueEntries.Select(q => q.Player).Where(p => !usedIds.Contains(p.PlayerId)).ToList();

            var random = new Random();
            var shuffled = remainingPlayers.OrderBy(_ => random.Next()).Take(2).ToList();

            for (int i = 0; i < shuffled.Count; i++)
            {
                _context.MatchPlayers.Add(new MatchPlayer
                {
                    MatchId = match.MatchId,
                    PlayerId = shuffled[i].PlayerId,
                    TeamNumber = 2
                });
            }

            // ✅ Remove from queue
            foreach (var entry in queueEntries.Where(q => requestedPairs.Any(r => r.PlayerId == q.PlayerId || r.PartnerId == q.PlayerId) || shuffled.Any(p => p.PlayerId == q.PlayerId)))
            {
                entry.Status = "InMatch";
                entry.Position = null;
            }

            await _context.SaveChangesAsync();
            return match;
        }

        private async Task<List<Match>> CreateMatchesForAllCourtsAsync(int sessionId, int numberOfCourts)
        {
            await UpdateQueuePrioritiesAsync(sessionId);

            var queueEntries = await _context.QueueEntries
                .Where(q => q.SessionId == sessionId && q.Status == "Waiting")
                .Include(q => q.Player)
                .OrderByDescending(q => q.PriorityScore)
                .ThenBy(q => q.CheckInTime)
                .ToListAsync();

            if (queueEntries.Count < numberOfCourts * 4) return new List<Match>();

            var recentMatches = await _context.PlayerMatchHistories
                .Where(h => h.PlayedAt >= DateTime.UtcNow.AddHours(-4))
                .ToListAsync();

            // Shuffle all players once
            var shuffled = queueEntries.Select(q => q.Player).OrderBy(_ => Guid.NewGuid()).ToList();

            var createdMatches = new List<Match>();
            int courtNumber = 1;

            for (int i = 0; i < numberOfCourts; i++)
            {
                var selectedPlayers = shuffled.Skip(i * 4).Take(4).ToList();

                var match = new Match
                {
                    SessionId = sessionId,
                    CourtNumber = courtNumber++,
                    RotationMode = "RandomMix",
                    StartTime = DateTime.UtcNow,
                    IsCompleted = false
                };

                _context.Matches.Add(match);
                await _context.SaveChangesAsync();

                // Assign teams (2 vs 2)
                for (int j = 0; j < 4; j++)
                {
                    _context.MatchPlayers.Add(new MatchPlayer
                    {
                        MatchId = match.MatchId,
                        PlayerId = selectedPlayers[j].PlayerId,
                        TeamNumber = j < 2 ? 1 : 2
                    });
                }

                // Remove from queue
                foreach (var entry in queueEntries.Where(q => selectedPlayers.Any(p => p.PlayerId == q.PlayerId)))
                {
                    entry.Status = "InMatch";
                    entry.Position = null;
                }

                createdMatches.Add(match);
            }

            await _context.SaveChangesAsync();
            return createdMatches;
        }


        private decimal CalculatePriority(QueueEntry entry)
        {
            var waiting = (decimal)(DateTime.UtcNow - entry.CheckInTime).TotalMinutes * 10m;
            var skill = entry.Player.SkillLevel * 30m;
            var winp = entry.Player.WinPercentage * 0.5m;
            return waiting + skill + winp;
        }

        private async Task<List<MatchPlayer>> GetMatchPlayersAsync(int matchId)
        {
            return await _context.MatchPlayers
                .Where(mp => mp.MatchId == matchId)
                .ToListAsync();
        }
    }
}
