using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/matches")]
    [Authorize]
    public class MatchController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchController> _logger;
        private readonly MixFlowDbContext _context;

        public MatchController(IMatchService matchService, IMapper mapper, ILogger<MatchController> logger, MixFlowDbContext context)
        {
            _matchService = matchService;
            _mapper = mapper;
            _logger = logger;
            _context = context;
        }

        // 1) Enqueue player
        [HttpPost("enqueue/{playerId}")]
        public async Task<ActionResult<QueueEntryDto>> EnqueuePlayer(int sessionId, int playerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entry = await _matchService.EnqueuePlayerAsync(sessionId, playerId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(_mapper.Map<QueueEntryDto>(entry));
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Failed to enqueue player {PlayerId} for session {SessionId}", playerId, sessionId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // 2) View queue
        [HttpGet("queue")]
        public async Task<ActionResult<List<QueueEntryDto>>> GetQueue(int sessionId)
        {
            var items = await _matchService.GetCurrentQueueAsync(sessionId);
            return Ok(_mapper.Map<List<QueueEntryDto>>(items));
        }

        // 3) Auto-match: fill courts from queue
        [HttpPost("auto-match")]
        public async Task<IActionResult> AutoMatch(int sessionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _matchService.AutoFillCourtsFromQueueAsync(sessionId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Auto-matching triggered." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = ex.Message });
            }
        }

        // 4) Manually create one match
        [HttpPost("smartmix/{sessionId}")]
        public async Task<ActionResult<MatchDto>> CreateSmartMix(int sessionId, [FromBody] List<(int PlayerId, int PartnerId)> requestedPairs)
        {
            var match = await _matchService.CreateNextSmartMixAsync(sessionId, requestedPairs);
            if (match == null) return BadRequest("Not enough players in queue.");
            return Ok(_mapper.Map<MatchDto>(match));
        }


        // 5) Record result for a specific match
        [HttpPost("record-result")]
        public async Task<IActionResult> RecordResult(int sessionId, [FromBody] MatchResultDto dto)
        {
            if (dto == null || dto.Team1PlayerIds.Count != 2 || dto.Team2PlayerIds.Count != 2)
                return BadRequest(new { error = "Provide 2 players in each team." });
            if (dto.Team1Score < 0 || dto.Team2Score < 0)
                return BadRequest(new { error = "Scores must be non-negative." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _matchService.RecordMatchResultAsync(
                    sessionId,
                    dto.CourtNumber,
                    dto.Team1Score,
                    dto.Team2Score,
                    dto.Team1PlayerIds,
                    dto.Team2PlayerIds
                );

                await _matchService.HandlePostMatchAsync(sessionId, dto.Team1PlayerIds.Concat(dto.Team2PlayerIds).ToList());

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Result recorded, stats updated, and next auto-match attempted.", matchId = match.MatchId });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to record result for session {SessionId}", sessionId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // 6) Remove player from queue
        [HttpDelete("queue/{playerId}")]
        public async Task<IActionResult> RemoveFromQueue(int sessionId, int playerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var removed = await _matchService.RemoveFromQueueAsync(sessionId, playerId);
            if (!removed)
            {
                await transaction.RollbackAsync();
                return NotFound(new { error = "Player not found in queue." });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { message = $"Player {playerId} removed from queue." });
        }

        // 7) Get completed matches
        [HttpGet("completed")]
        public async Task<ActionResult<List<MatchDto>>> GetCompletedMatches(int sessionId)
        {
            var matches = await _matchService.GetSessionMatchesAsync(sessionId);
            return Ok(_mapper.Map<List<MatchDto>>(matches));
        }

        // 8) Get active matches
        [HttpGet("active")]
        public async Task<ActionResult<List<MatchDto>>> GetActiveMatches(int sessionId)
        {
            var matches = await _matchService.GetActiveMatchesAsync(sessionId);
            return Ok(_mapper.Map<List<MatchDto>>(matches));
        }
    }
}
