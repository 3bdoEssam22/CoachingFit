namespace CoachingFit.Identity.Core.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public string TokenHash { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime AbsoluteExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokeReason { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public bool IsActiveAt(DateTimeOffset now) =>
            RevokedAt is null
            && now.UtcDateTime < ExpiresAt
            && now.UtcDateTime < AbsoluteExpiresAt;
    }
}
