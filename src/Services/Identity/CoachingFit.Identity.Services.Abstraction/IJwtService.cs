using CoachingFit.Identity.Core.Entities;

namespace CoachingFit.Identity.Services.Abstraction
{
    public interface IJwtService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, string role);
    }
}
