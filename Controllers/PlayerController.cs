using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.PlayerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Models;
using System.Security.Claims;

namespace MixFlowWebApp.Controllers
{
    [ApiController]
    [Route("api/players")]
    [Authorize]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly IOrganizerService _organizerService;
        private readonly IMapper _mapper;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(
            IPlayerService playerService,
            IOrganizerService organizerService,
            IMapper mapper,
            ILogger<PlayerController> logger)
        {
            _playerService = playerService;
            _organizerService = organizerService;
            _mapper = mapper;
            _logger = logger;
        }

        // Every action here scopes to whichever organizer is logged in — players belong
        // to exactly one organizer's roster and are never shared across accounts.
        private async Task<Organizer?> GetCurrentOrganizerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;
            return await _organizerService.GetOrganizerByUserIdAsync(userId);
        }

        /// Create a new player.
        [HttpPost]
        public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerDto dto)
        {
            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            try
            {
                var playerEntity = _mapper.Map<Player>(dto); // map DTO → entity
                var player = await _playerService.AddPlayerAsync(organizer.OrganizerId, playerEntity); // service returns entity
                var playerDto = _mapper.Map<PlayerDto>(player); // map entity → DTO

                _logger.LogInformation("Player {PlayerId} created for Organizer {OrganizerId}", player.PlayerId, organizer.OrganizerId);
                return CreatedAtAction(nameof(GetPlayerById), new { id = player.PlayerId }, playerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating player");
                return BadRequest(new { error = "Failed to create player" });
            }
        }

        /// Get a player by ID — only if they belong to the current organizer.
        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDto>> GetPlayerById(int id)
        {
            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var player = await _playerService.GetPlayerByIdAsync(organizer.OrganizerId, id);
            if (player == null)
            {
                _logger.LogWarning("Player {PlayerId} not found for Organizer {OrganizerId}", id, organizer.OrganizerId);
                return NotFound(new { error = $"Player with ID {id} not found" });
            }

            return Ok(_mapper.Map<PlayerDto>(player));
        }

        /// Get every player belonging to the current organizer — never another
        /// organizer's roster.
        [HttpGet]
        public async Task<ActionResult<List<PlayerDto>>> GetAllPlayers()
        {
            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var players = await _playerService.GetPlayersByOrganizerAsync(organizer.OrganizerId);
            var playerDtos = _mapper.Map<List<PlayerDto>>(players);

            _logger.LogInformation("Retrieved {Count} players for Organizer {OrganizerId}", playerDtos.Count, organizer.OrganizerId);
            return Ok(playerDtos);
        }

        /// Update a player — only if they belong to the current organizer.
        [HttpPut("{id}")]
        public async Task<ActionResult<PlayerDto>> UpdatePlayer(int id, [FromBody] UpdatePlayerDto dto)
        {
            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var updatedEntity = _mapper.Map<Player>(dto); // map DTO → entity
            var player = await _playerService.UpdatePlayerAsync(organizer.OrganizerId, id, updatedEntity); // service returns entity
            if (player == null)
            {
                _logger.LogWarning("Failed to update player {PlayerId} for Organizer {OrganizerId} - not found", id, organizer.OrganizerId);
                return NotFound(new { error = $"Player with ID {id} not found" });
            }

            var playerDto = _mapper.Map<PlayerDto>(player);
            _logger.LogInformation("Player {PlayerId} updated successfully", id);
            return Ok(playerDto);
        }

        /// Delete a player — only if they belong to the current organizer.
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlayer(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid player ID {PlayerId} for delete", id);
                return BadRequest(new { error = "Invalid player ID" });
            }

            var organizer = await GetCurrentOrganizerAsync();
            if (organizer == null) return Unauthorized(new { error = "Organizer account not found" });

            var result = await _playerService.DeletePlayerAsync(organizer.OrganizerId, id);
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