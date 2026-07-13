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

        /// Sign up: creates a brand-new organizer account from Google identity.
        /// Fails with 409 Conflict if an organizer already exists for this UserId/Email.
        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.Email))
                return BadRequest(new { error = "Invalid Google token data" });

            var organizer = await _organizerService.CreateOrganizerAsync(
                dto.UserId, dto.FullName ?? dto.Email.Split('@')[0], dto.Email);

            if (organizer == null)
            {
                return Conflict(new { error = "An account with this Google identity already exists. Please log in instead." });
            }

            var token = _jwtService.GenerateToken(organizer);
            var organizerDto = _mapper.Map<OrganizerDto>(organizer);

            _logger.LogInformation("New organizer {OrganizerId} signed up", organizer.OrganizerId);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Organizer = organizerDto,
                Message = "Signup successful"
            });
        }

        /// Log in: looks up an existing organizer only — never creates one.
        /// Fails with 404 if no organizer exists yet for this Google identity.
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.Email))
                return BadRequest(new { error = "Invalid Google token data" });

            var organizer = await _organizerService.GetOrganizerForLoginAsync(dto.UserId, dto.Email);

            if (organizer == null)
            {
                return NotFound(new { error = "No account found for this Google identity. Please sign up first." });
            }

            var token = _jwtService.GenerateToken(organizer);
            var organizerDto = _mapper.Map<OrganizerDto>(organizer);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Organizer = organizerDto,
                Message = "Login successful"
            });
        }

        /// Convenience login by email only. No password, no verification of any kind —
        /// anyone who knows an organizer's email can log in as them.
        [HttpPost("email-login")]
        public async Task<ActionResult<AuthResponseDto>> EmailLogin([FromBody] EmailLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { error = "Email is required" });

            var organizer = await _organizerService.GetOrganizerForLoginAsync(string.Empty, dto.Email);

            if (organizer == null)
                return NotFound(new { error = "No account found for this email. Please sign up first." });

            var token = _jwtService.GenerateToken(organizer);
            var organizerDto = _mapper.Map<OrganizerDto>(organizer);

            _logger.LogWarning("Email-only login (no verification) used for organizer {OrganizerId}", organizer.OrganizerId);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Organizer = organizerDto,
                Message = "Login successful"
            });
        }

        /// Convenience signup by email only. No password, no verification of any kind.
        /// A UserId is still generated automatically since Organizer.UserId is required.
        [HttpPost("email-signup")]
        public async Task<ActionResult<AuthResponseDto>> EmailSignup([FromBody] EmailSignupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(new { error = "Full name and email are required" });

            var syntheticUserId = $"email-{Guid.NewGuid()}";
            var organizer = await _organizerService.CreateOrganizerAsync(syntheticUserId, dto.FullName, dto.Email);

            if (organizer == null)
                return Conflict(new { error = "An account with this email already exists. Please log in instead." });

            var token = _jwtService.GenerateToken(organizer);
            var organizerDto = _mapper.Map<OrganizerDto>(organizer);

            _logger.LogWarning("Email-only signup (no verification) used for new organizer {OrganizerId}", organizer.OrganizerId);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Organizer = organizerDto,
                Message = "Signup successful!"
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