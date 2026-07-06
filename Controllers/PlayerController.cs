using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.PlayerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/players")]
    [Authorize]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(IPlayerService playerService, IMapper mapper, ILogger<PlayerController> logger)
        {
            _playerService = playerService;
            _mapper = mapper;
            _logger = logger;
        }

        /// Create a new player.
        [HttpPost]
        public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerDto dto)
        {
            try
            {
                var playerEntity = _mapper.Map<Player>(dto); // map DTO → entity
                var player = await _playerService.AddPlayerAsync(playerEntity); // service returns entity
                var playerDto = _mapper.Map<PlayerDto>(player); // map entity → DTO

                _logger.LogInformation("Player {PlayerId} created successfully", player.PlayerId);
                return CreatedAtAction(nameof(GetPlayerById), new { id = player.PlayerId }, playerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating player");
                return BadRequest(new { error = "Failed to create player" });
            }
        }

        /// Get a player by ID.
        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDto>> GetPlayerById(int id)
        {
            var player = await _playerService.GetPlayerByIdAsync(id);
            if (player == null)
            {
                _logger.LogWarning("Player {PlayerId} not found", id);
                return NotFound(new { error = $"Player with ID {id} not found" });
            }

            _logger.LogInformation("Player {PlayerId} retrieved", id);
            return Ok(_mapper.Map<PlayerDto>(player));
        }

        /// Get all players.
        [HttpGet]
        public async Task<ActionResult<List<PlayerDto>>> GetAllPlayers()
        {
            var players = await _playerService.GetAllPlayersAsync();
            var playerDtos = _mapper.Map<List<PlayerDto>>(players);

            _logger.LogInformation("Retrieved {Count} players", playerDtos.Count);
            return Ok(playerDtos);
        }

        /// Update a player.
        [HttpPut("{id}")]
        public async Task<ActionResult<PlayerDto>> UpdatePlayer(int id, [FromBody] UpdatePlayerDto dto)
        {
            var updatedEntity = _mapper.Map<Player>(dto); // map DTO → entity
            var player = await _playerService.UpdatePlayerAsync(id, updatedEntity); // service returns entity
            if (player == null)
            {
                _logger.LogWarning("Failed to update player {PlayerId} - not found", id);
                return NotFound(new { error = $"Player with ID {id} not found" });
            }

            var playerDto = _mapper.Map<PlayerDto>(player);
            _logger.LogInformation("Player {PlayerId} updated successfully", id);
            return Ok(playerDto);
        }

        /// Delete a player.
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlayer(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid player ID {PlayerId} for delete", id);
                return BadRequest(new { error = "Invalid player ID" });
            }

            var result = await _playerService.DeletePlayerAsync(id);
            if (!result)
            {
                _logger.LogWarning("Player {PlayerId} not found for delete", id);
                return NotFound(new { error = $"Player with ID {id} not found" });
            }

            _logger.LogInformation("Player {PlayerId} deleted successfully", id);
            return NoContent();
        }
    }
}
