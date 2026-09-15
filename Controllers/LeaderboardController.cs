using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;
using System.Security.Claims;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        private async Task<Models.Organizer?> GetCurrentOrganizerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;
            return await _organizerService.GetOrganizerByUserIdAsync(userId);
        }

        // Session leaderboard endpoint — only for a session the current organizer owns.
        [HttpGet("session/{sessionId:int}")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetSessionLeaderboard(int sessionId)
        {
            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session == null || session.OrganizerId != organizer.OrganizerId)
                return NotFound(new { error = $"Session with ID {sessionId} not found" });

            var players = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
        }
    }
}