using Microsoft.AspNetCore.Identity;

namespace CoachingFit.Identity.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

        public string FullName => $"{FirstName} {LastName}";
    }
}
