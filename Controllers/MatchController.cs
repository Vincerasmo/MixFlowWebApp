using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Data;
using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.DTOs.SessionPlayerDTOs;
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
        private readonly ISessionPlayerService _sessionPlayerService;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchController> _logger;
        private readonly MixFlowDbContext _context;

        public MatchController(
            IMatchService matchService,
            ISessionPlayerService sessionPlayerService,
            IMapper mapper,
            ILogger<MatchController> logger,
            MixFlowDbContext context)
        {
            _matchService = matchService;
            _sessionPlayerService = sessionPlayerService;
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
        // Smart Mix — manual: organizer-selected pairs, targeted at one specific court.
        [HttpPost("court/{courtNumber}/manual-mix")]
        public async Task<ActionResult<MatchDto>> ManualMixCourt(int sessionId, int courtNumber, [FromBody] SmartMixRequestDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _matchService.CreateManualMatchForCourtAsync(sessionId, courtNumber, dto.Pairs);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var full = await _context.Matches
                    .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
                    .FirstAsync(m => m.MatchId == match.MatchId);

                return Ok(_mapper.Map<MatchDto>(full));
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = ex.Message });
            }
        }

        // 4.5) Fill exactly one specific court from the queue — unlike auto-match, this
        // never touches any other court, even if several are free at once.
        [HttpPost("court/{courtNumber}/smart-mix")]
        public async Task<ActionResult<MatchDto>> SmartMixCourt(int sessionId, int courtNumber)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _matchService.CreateMatchForCourtAsync(sessionId, courtNumber);
                if (match == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { error = "Not enough players in the queue to fill this court (need at least 4)." });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var full = await _context.Matches
                    .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
                    .FirstAsync(m => m.MatchId == match.MatchId);

                return Ok(_mapper.Map<MatchDto>(full));
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Failed to smart-mix court {CourtNumber} for session {SessionId}", courtNumber, sessionId);
                return BadRequest(new { error = ex.Message });
            }
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
                // 🐛 FIX: RecordMatchResultAsync already calls HandlePostMatchAsync internally
                // (it did before this change too) — this controller was calling it a SECOND
                // time right after, which redundantly re-ran queue priority updates and
                // auto-fill for every single result. Removed the duplicate call; the service
                // call below is the only one now.
                var match = await _matchService.RecordMatchResultAsync(
                    sessionId,
                    dto.CourtNumber,
                    dto.Team1Score,
                    dto.Team2Score,
                    dto.Team1PlayerIds,
                    dto.Team2PlayerIds
                );

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

        // 9) Bench a player — also pulls them out of the queue if they were waiting,
        // so a benched player can never still show up as "in line" for a match.
        [HttpPost("bench")]
        public async Task<ActionResult> BenchPlayer(int sessionId, [FromBody] BenchPlayerDto dto)
        {
            if (sessionId <= 0) return BadRequest(new { error = "Invalid session ID" });
            if (!ModelState.IsValid) return BadRequest(new { error = "Invalid input" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await _sessionPlayerService.BenchPlayerAsync(sessionId, dto.PlayerId, dto.Reason);
            if (result == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { error = "Player not found in this session" });
            }

            await _matchService.RemoveFromQueueAsync(sessionId, dto.PlayerId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Player {PlayerId} benched in Session {SessionId}", dto.PlayerId, sessionId);
            return Ok(new { message = "Player benched successfully", reason = dto.Reason });
        }

        // 10) Return a player from bench
        [HttpPost("bench/{playerId}/return")]
        public async Task<ActionResult> ReturnFromBench(int sessionId, int playerId)
        {
            if (sessionId <= 0 || playerId <= 0) return BadRequest(new { error = "Invalid session ID or player ID" });

            var result = await _sessionPlayerService.ReturnFromBenchAsync(sessionId, playerId);
            if (result == null) return NotFound(new { error = "Player not found in this session or not benched" });

            _logger.LogInformation("Player {PlayerId} returned from bench in Session {SessionId}", playerId, sessionId);
            return Ok(new { message = "Player returned from bench successfully" });
        }

        // 10.5) Return a player from bench straight back into the queue, atomically —
        // if either step fails, neither happens, so a player can never end up stuck
        // as "Available" when the intent was "Waiting".
        [HttpPost("bench/{playerId}/return-to-queue")]
        public async Task<ActionResult<QueueEntryDto>> ReturnToQueue(int sessionId, int playerId)
        {
            if (sessionId <= 0 || playerId <= 0) return BadRequest(new { error = "Invalid session ID or player ID" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var returned = await _sessionPlayerService.ReturnFromBenchAsync(sessionId, playerId);
                if (returned == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { error = "Player not found in this session or not benched" });
                }

                var entry = await _matchService.EnqueuePlayerAsync(sessionId, playerId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Player {PlayerId} returned from bench and re-queued in Session {SessionId}", playerId, sessionId);
                return Ok(_mapper.Map<QueueEntryDto>(entry));
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Failed to return player {PlayerId} to queue for session {SessionId}", playerId, sessionId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // 11) Get benched players
        [HttpGet("benched")]
        public async Task<ActionResult<List<SessionPlayerDto>>> GetBenchedPlayers(int sessionId)
        {
            if (sessionId <= 0) return BadRequest(new { error = "Invalid session ID" });

            var benched = await _sessionPlayerService.GetBenchPlayersAsync(sessionId);
            return Ok(_mapper.Map<List<SessionPlayerDto>>(benched));
        }
    }
}