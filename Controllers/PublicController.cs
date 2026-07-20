using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.DTOs.MatchDTOs;
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
                status = session.Status
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
    }
}