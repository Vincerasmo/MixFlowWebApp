using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.Interfaces.Services;

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

        // Session leaderboard endpoint
        [HttpGet("session/{sessionId:int}")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetSessionLeaderboard(int sessionId)
        {
            var players = await _leaderboardService.GetSessionLeaderboardAsync(sessionId);
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
        }

        // Overall leaderboard endpoint
        [HttpGet("overall")]
        public async Task<ActionResult<List<LeaderboardPlayerDto>>> GetOverallLeaderboard()
        {
            var players = await _leaderboardService.GetOverallLeaderboardAsync();
            var dtoList = _mapper.Map<List<LeaderboardPlayerDto>>(players);
            return Ok(dtoList);
        }
    }
}