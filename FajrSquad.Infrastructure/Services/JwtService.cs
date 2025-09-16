using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FajrSquad.Core.Config;
using FajrSquad.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FajrSquad.Infrastructure.Services
{
    public class JwtService
    {
        private readonly JwtSettings _settings;

        public JwtService(IOptions<JwtSettings> settings) => _settings = settings.Value;

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.Name),
                new Claim("phone", user.Phone),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("city", user.City ?? "Roma"),
                new("country", user.Country)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(string? createdByIp = null)
        {
            // 64 bytes -> ~ 86 char Base64, robusto e imprevedibile
            var bytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(bytes);

            return new RefreshToken
            {
                Token = token,
                Expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays),
                Created = DateTime.UtcNow,
                CreatedByIp = createdByIp ?? "unknown"
            };
        }
    }
}
