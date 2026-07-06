using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.SessionDTOs;
using MixFlowWebApp.DTOs.SessionPlayerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;
using System.Security.Claims;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IOrganizerService _organizerService;
        private readonly ISessionPlayerService _sessionPlayerService;
        private readonly IMapper _mapper;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            ISessionService sessionService,
            IOrganizerService organizerService,
            ISessionPlayerService sessionPlayerService,
            IMapper mapper,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _organizerService = organizerService;
            _sessionPlayerService = sessionPlayerService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Session Management

        [HttpPost]
        public async Task<ActionResult<SessionDto>> CreateSession([FromBody] CreateSessionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(new { error = "Invalid input" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "User not authenticated" });

            var organizer = await _organizerService.GetOrganizerByUserIdAsync(userId);
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var sessionEntity = _mapper.Map<Session>(dto);
            var session = await _sessionService.CreateSessionAsync(organizer.OrganizerId, sessionEntity);

            _logger.LogInformation("Session {SessionId} created by Organizer {OrganizerId}", session.SessionId, organizer.OrganizerId);
            return CreatedAtAction(nameof(GetSessionById), new { id = session.SessionId }, _mapper.Map<SessionDto>(session));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SessionDto>> GetSessionById(int id)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid session ID" });

            var session = await _sessionService.GetSessionByIdAsync(id);
            if (session == null) return NotFound(new { error = $"Session with ID {id} not found" });

            return Ok(_mapper.Map<SessionDto>(session));
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<SessionDto>>> GetMySessions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "User not authenticated" });

            var organizer = await _organizerService.GetOrganizerByUserIdAsync(userId);
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var sessions = await _sessionService.GetOrganizerSessionsAsync(organizer.OrganizerId);
            return Ok(_mapper.Map<List<SessionDto>>(sessions));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateSession(int id, [FromBody] UpdateSessionDto dto)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid session ID" });
            if (!ModelState.IsValid) return BadRequest(new { error = "Invalid input" });

            var updatedEntity = _mapper.Map<Session>(dto);
            var session = await _sessionService.UpdateSessionAsync(id, updatedEntity);
            if (session == null) return NotFound(new { error = $"Session with ID {id} not found" });

            _logger.LogInformation("Session {SessionId} updated", id);
            return Ok(_mapper.Map<SessionDto>(session));
        }

        [HttpPost("{id}/end")]
        public async Task<ActionResult> EndSession(int id)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid session ID" });

            var result = await _sessionService.EndSessionAsync(id);
            if (!result) return NotFound(new { error = $"Session with ID {id} not found" });

            _logger.LogInformation("Session {SessionId} ended", id);
            return Ok(new { message = "Session ended successfully" });
        }

        #endregion

        #region Player Management in Session

        [HttpPost("{sessionId}/players/{playerId}")]
        public async Task<ActionResult<SessionPlayerDto>> AddPlayerToSession(int sessionId, int playerId)
        {
            if (sessionId <= 0 || playerId <= 0) return BadRequest(new { error = "Invalid session ID or player ID" });

            var sessionPlayer = await _sessionPlayerService.AddPlayerToSessionAsync(sessionId, playerId);
            if (sessionPlayer == null) return NotFound(new { error = "Session or player not found" });

            _logger.LogInformation("Player {PlayerId} added to Session {SessionId}", playerId, sessionId);
            return CreatedAtAction(nameof(GetSessionPlayers), new { sessionId }, _mapper.Map<SessionPlayerDto>(sessionPlayer));
        }

        [HttpGet("{sessionId}/players")]
        public async Task<ActionResult<List<SessionPlayerDto>>> GetSessionPlayers(int sessionId)
        {
            if (sessionId <= 0) return BadRequest(new { error = "Invalid session ID" });

            var players = await _sessionPlayerService.GetSessionPlayersAsync(sessionId);
            return Ok(_mapper.Map<List<SessionPlayerDto>>(players));
        }

        [HttpPost("{sessionId}/players/bench")]
        public async Task<ActionResult> BenchPlayer(int sessionId, [FromBody] BenchPlayerDto dto)
        {
            if (sessionId <= 0) return BadRequest(new { error = "Invalid session ID" });
            if (!ModelState.IsValid) return BadRequest(new { error = "Invalid input" });

            var result = await _sessionPlayerService.BenchPlayerAsync(sessionId, dto.PlayerId, dto.Reason);
            if (result == null) return NotFound(new { error = "Player not found in this session" });

            _logger.LogInformation("Player {PlayerId} benched in Session {SessionId}", dto.PlayerId, sessionId);
            return Ok(new { message = "Player benched successfully", reason = dto.Reason });
        }

        [HttpPost("{sessionId}/players/{playerId}/return")]
        public async Task<ActionResult> ReturnFromBench(int sessionId, int playerId)
        {
            if (sessionId <= 0 || playerId <= 0) return BadRequest(new { error = "Invalid session ID or player ID" });

            var result = await _sessionPlayerService.ReturnFromBenchAsync(sessionId, playerId);
            if (result == null) return NotFound(new { error = "Player not found in this session or not benched" });

            _logger.LogInformation("Player {PlayerId} returned from bench in Session {SessionId}", playerId, sessionId);
            return Ok(new { message = "Player returned from bench successfully" });
        }

        [HttpGet("{sessionId}/players/benched")]
        public async Task<ActionResult<List<SessionPlayerDto>>> GetBenchedPlayers(int sessionId)
        {
            if (sessionId <= 0) return BadRequest(new { error = "Invalid session ID" });

            var benched = await _sessionPlayerService.GetBenchPlayersAsync(sessionId);
            return Ok(_mapper.Map<List<SessionPlayerDto>>(benched));
        }

        [HttpDelete("{sessionId}/players/{playerId}")]
        public async Task<ActionResult> RemovePlayerFromSession(int sessionId, int playerId)
        {
            if (sessionId <= 0 || playerId <= 0)
                return BadRequest(new { error = "Invalid session ID or player ID" });

            var result = await _sessionPlayerService.RemovePlayerFromSessionAsync(sessionId, playerId);
            if (!result)
            {
                _logger.LogWarning("Failed to remove Player {PlayerId} from Session {SessionId} - not found", playerId, sessionId);
                return NotFound(new { error = "Player not found in this session" });
            }

            _logger.LogInformation("Player {PlayerId} removed from Session {SessionId}", playerId, sessionId);
            return NoContent();
        }

        #endregion
    }
}
