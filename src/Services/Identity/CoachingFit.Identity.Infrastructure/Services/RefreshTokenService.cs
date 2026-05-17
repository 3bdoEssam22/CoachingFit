using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Infrastructure.Data.Context;
using CoachingFit.Identity.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace CoachingFit.Identity.Infrastructure.Services
{
    public class RefreshTokenService(
        IdentityDbContext _db,
        TimeProvider _timeProvider,
        IConfiguration _configuration) : IRefreshTokenService
    {
        public async Task<(string PlaintextToken, RefreshToken Entity)> IssueAsync(
            string userId, CancellationToken ct = default)
        {
            var slidingDays = double.Parse(_configuration["Jwt:RefreshTokenDurationInDays"]
                ?? throw new InvalidOperationException("Jwt:RefreshTokenDurationInDays is not configured."));
            var absoluteDays = double.Parse(_configuration["Jwt:RefreshTokenAbsoluteExpiryInDays"]
                ?? throw new InvalidOperationException("Jwt:RefreshTokenAbsoluteExpiryInDays is not configured."));
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var (plaintext, hash) = GenerateTokenPair();

            var entity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(slidingDays),
                AbsoluteExpiresAt = now.AddDays(absoluteDays)
            };

            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync(ct);

            return (plaintext, entity);
        }

        public Task<RefreshToken?> FindByPlaintextAsync(
            string plaintext, CancellationToken ct = default)
        {
            var hash = HashToken(plaintext);
            return _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        }

        public async Task<(string PlaintextToken, RefreshToken Entity)> RotateAsync(
            RefreshToken current, CancellationToken ct = default)
        {
            var slidingDays = double.Parse(_configuration["Jwt:RefreshTokenDurationInDays"]!);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var (plaintext, hash) = GenerateTokenPair();

            var newEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = current.UserId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(slidingDays),
                AbsoluteExpiresAt = current.AbsoluteExpiresAt
            };

            current.RevokedAt = now;
            current.RevokeReason = "rotated";
            current.ReplacedByTokenHash = hash;

            _db.RefreshTokens.Add(newEntity);
            await _db.SaveChangesAsync(ct);

            return (plaintext, newEntity);
        }

        public Task RevokeAsync(RefreshToken token, string reason, CancellationToken ct = default)
        {
            token.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
            token.RevokeReason = reason;
            return _db.SaveChangesAsync(ct);
        }

        public Task RevokeAllForUserAsync(string userId, string reason, CancellationToken ct = default)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.RevokeReason, reason), ct);
        }

        private static (string Plaintext, string Hash) GenerateTokenPair()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var plaintext = Base64UrlEncoder.Encode(bytes);
            var hash = HashToken(plaintext);
            return (plaintext, hash);
        }

        private static string HashToken(string plaintext)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
            return Convert.ToHexString(hashBytes);
        }
    }
}
