using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.PublicDTOs;
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.Interfaces.Services;

namespace MixFlowWebApp.Controllers
{
    // No [Authorize] anywhere in this controller — every action here is intentionally
    // public and read-only, for the spectator "watch" link players open with no login
    // (see WatchPage.tsx / /watch/:sessionId on the frontend). Never add a write action
    // to this controller; if it needs a POST/PUT/DELETE, it belongs in an authenticated
    // controller instead.
    [ApiController]
    [Route("api/public/sessions")]
    public class PublicController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IMatchService _matchService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly IMapper _mapper;

        public PublicController(
            ISessionService sessionService,
            IMatchService matchService,
            ILeaderboardService leaderboardService,
            IMapper mapper)
        {
            _sessionService = sessionService;
            _matchService = matchService;
            _leaderboardService = leaderboardService;
            _mapper = mapper;
        }

        // Deliberately NOT mapping the full SessionDto here — only the fields a spectator
        // actually needs. SessionDto may carry organizer-facing fields over time that
        // shouldn't be exposed on a link anyone can open without logging in.
        [HttpGet("{sessionId}")]
        public async Task<ActionResult> GetSession(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            return Ok(new
            {
                sessionId = session.SessionId,
                sessionName = session.SessionName,
                numberOfCourts = session.NumberOfCourts,
                status = session.Status,
                startTime = session.StartTime,
                endTime = session.EndTime
            });
        }

        [HttpGet("{sessionId}/queue")]
        public async Task<ActionResult<List<QueueEntryDto>>> GetQueue(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var queue = await _matchService.GetCurrentQueueAsync(sessionId);
            return Ok(_mapper.Map<List<QueueEntryDto>>(queue));
        }

        [HttpGet("{sessionId}/matches/active")]
        public async Task<ActionResult<List<MatchDto>>> GetActiveMatches(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var matches = await _matchService.GetActiveMatchesAsync(sessionId);
            return Ok(_mapper.Map<List<MatchDto>>(matches));
        }

        // NEW: the prepared, not-yet-started "next up" matches (target: 2) — same data
        // the organizer's Queue page shows, just read-only here.
        [HttpGet("{sessionId}/matches/next-up")]
        public async Task<ActionResult<List<MatchDto>>> GetNextUpMatches(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var matches = await _matchService.GetNextUpMatchesAsync(sessionId);
            return Ok(_mapper.Map<List<MatchDto>>(matches));
        }

        [HttpGet("{sessionId}/matches/completed")]
        public async Task<ActionResult<List<MatchDto>>> GetCompletedMatches(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var matches = await _matchService.GetSessionMatchesAsync(sessionId);
            return Ok(_mapper.Map<List<MatchDto>>(matches));
        }

        [HttpGet("{sessionId}/leaderboard")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetLeaderboard(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var leaderboard = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);
            return Ok(leaderboard);
        }

        // Post-session wrap-up for the public /report/:sessionId page — a richer,
        // shareable summary distinct from the live Watch page: MVP callout, final
        // standings, and a "biggest win" highlight, on top of the same match list
        // Watch already shows once a session ends.
        [HttpGet("{sessionId}/report")]
        public async Task<ActionResult<SessionReportDto>> GetSessionReport(int sessionId)
        {
            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var completedMatches = await _matchService.GetSessionMatchesAsync(sessionId);
            var standings = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);

            var totalDistinctPlayers = completedMatches
                .SelectMany(m => m.MatchPlayers)
                .Select(mp => mp.PlayerId)
                .Distinct()
                .Count();

            // Largest |Team1Score - Team2Score| among completed matches with both scores
            // recorded. Ties broken by whichever match comes first in the already-applied
            // "most recent first" ordering from GetSessionMatchesAsync.
            var biggestWinMatch = completedMatches
                .Where(m => m.Team1Score.HasValue && m.Team2Score.HasValue)
                .OrderByDescending(m => Math.Abs(m.Team1Score!.Value - m.Team2Score!.Value))
                .FirstOrDefault();

            var report = new SessionReportDto
            {
                SessionId = session.SessionId,
                SessionName = session.SessionName,
                SessionDate = session.SessionDate,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                NumberOfCourts = session.NumberOfCourts,
                Status = session.Status,
                TotalMatchesPlayed = completedMatches.Count,
                TotalDistinctPlayers = totalDistinctPlayers,
                FinalStandings = standings,
                Mvp = standings.FirstOrDefault(p => p.Rank == 1),
                BiggestWin = biggestWinMatch != null ? _mapper.Map<MatchDto>(biggestWinMatch) : null,
                BiggestWinMargin = biggestWinMatch != null
                    ? Math.Abs(biggestWinMatch.Team1Score!.Value - biggestWinMatch.Team2Score!.Value)
                    : 0
            };

            return Ok(report);
        }
    }
}