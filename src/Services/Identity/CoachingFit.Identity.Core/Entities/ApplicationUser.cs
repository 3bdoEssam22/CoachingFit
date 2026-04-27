using CoachingFit.Identity.Core.Enums;
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
        public UserRole UserRole { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
