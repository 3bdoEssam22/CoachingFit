using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoachingFit.Identity.Infrastructure.Services
{
    public class JwtService(IConfiguration _configuration) : IJwtService
    {
        public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, string role)
        {
            var expiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_configuration["Jwt:DurationInDays"]!));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,        user.Id),
                new(ClaimTypes.NameIdentifier,          user.Id),
                new(JwtRegisteredClaimNames.Email,      user.Email!),
                new(JwtRegisteredClaimNames.GivenName,  user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
                new(ClaimTypes.Role,                    role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}