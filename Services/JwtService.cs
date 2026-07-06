using Microsoft.IdentityModel.Tokens;
using MixFlowWebApp.Interfaces.Services.Auth;
using MixFlowWebApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MixFlowWebApp.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Organizer organizer)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, organizer.UserId),
                new Claim(ClaimTypes.Name, organizer.FullName),
                new Claim(ClaimTypes.Email, organizer.Email),
                new Claim("OrganizerId", organizer.OrganizerId.ToString()),
                new Claim(ClaimTypes.Role, "Organizer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopmentOnly1234567890"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // 8 hours token life
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return tokenHandler.ValidateToken(token, new TokenValidationParameters(), out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
