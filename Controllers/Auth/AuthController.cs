using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MixFlowWebApp.DTOs.AuthDTOs;
using MixFlowWebApp.DTOs.OrganizerDTOs;
using MixFlowWebApp.Interfaces.Services;
using MixFlowWebApp.Interfaces.Services.Auth;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MixFlowWebApp.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IOrganizerService _organizerService;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IOrganizerService organizerService, IJwtService jwtService, IMapper mapper, ILogger<AuthController> logger)
        {
            _organizerService = organizerService;
            _jwtService = jwtService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("google")]
        public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.Email))
                return BadRequest(new { error = "Invalid Google token data" });

            var organizer = await _organizerService.GetOrCreateOrganizerAsync(dto.UserId, dto.FullName ?? dto.Email.Split('@')[0], dto.Email);
            var token = _jwtService.GenerateToken(organizer);

            var organizerDto = _mapper.Map<OrganizerDto>(organizer);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Organizer = organizerDto,
                Message = "Login successful"
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<OrganizerDto>> GetCurrentOrganizer()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { error = "User not authenticated" });

            var organizer = await _organizerService.GetOrganizerByUserIdAsync(userId);
            if (organizer == null) return NotFound(new { error = "Organizer not found" });

            var organizerDto = _mapper.Map<OrganizerDto>(organizer);
            return Ok(organizerDto);
        }
    }
}
