using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;
        private readonly ISessionService _sessionService;
        private readonly IOrganizerService _organizerService;
        private readonly IMapper _mapper;

        public LeaderboardController(
            ILeaderboardService leaderboardService,
            ISessionService sessionService,
            IOrganizerService organizerService,
            IMapper mapper)
        {
            _leaderboardService = leaderboardService;
            _sessionService = sessionService;
            _organizerService = organizerService;
            _mapper = mapper;
        }

        // ✅ Session leaderboard endpoint
        // :int constraint keeps this from ever swallowing the literal "session/active" route below.
        [HttpGet("session/{sessionId:int}")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetSessionLeaderboard(int sessionId)
        {
            var players = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
        }

        // ✅ NEW: leaderboard for whichever session is currently active for this organizer.
        // Lets the Leaderboard page load without the frontend already knowing a sessionId —
        // it just asks "show me the leaderboard for whatever's happening right now".
        [HttpGet("session/active")]
        [Authorize]
        public async Task<ActionResult> GetActiveSessionLeaderboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "User not authenticated" });

            var organizer = await _organizerService.GetOrganizerByUserIdAsync(userId);
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var activeSession = await _sessionService.GetActiveSessionByOrganizerAsync(organizer.OrganizerId);
            if (activeSession == null) return NotFound(new { error = "No active session found." });

            var players = await _leaderboardService.GetSessionLeaderboardAsync(activeSession.SessionId);
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);

            return Ok(new
            {
                sessionId = activeSession.SessionId,
                sessionName = activeSession.SessionName,
                leaderboard = dtoList
            });
        }

        // ✅ Overall leaderboard endpoint
        [HttpGet("overall")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetOverallLeaderboard()
        {
            var players = await _leaderboardService.GetOverallLeaderboardAsync();
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
        }

        // ✅ Snapshot creation endpoint
        [HttpPost("session/{sessionId}/snapshot")]
        public async Task<IActionResult> SaveSessionLeaderboardSnapshot(int sessionId)
        {
            await _leaderboardService.SaveSessionLeaderboardSnapshotAsync(sessionId);
            return Ok(new { Message = $"Snapshot saved for session {sessionId}." });
        }

        // ✅ Retrieve snapshot history for a session
        [HttpGet("session/{sessionId}/history")]
        public async Task<ActionResult<List<LeaderboardDto>>> GetSessionLeaderboardHistory(int sessionId)
        {
            var snapshots = await _leaderboardService.GetSessionLeaderboardSnapshotsAsync(sessionId);

            var dtoList = snapshots.Select(l => new LeaderboardDto
            {
                SessionLeaders = _mapper.Map<List<LeaderboardPlayerDto>>(l.Entries.Select(e => e.Player)),
                OverallLeaders = new List<LeaderboardPlayerDto>() // not used here
            }).ToList();

            return Ok(dtoList);
        }
    }
}