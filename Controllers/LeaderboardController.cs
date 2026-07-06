using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;
        private readonly IMapper _mapper;

        public LeaderboardController(ILeaderboardService leaderboardService, IMapper mapper)
        {
            _leaderboardService = leaderboardService;
            _mapper = mapper;
        }

        // ✅ Session leaderboard endpoint
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetSessionLeaderboard(int sessionId)
        {
            var players = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
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
