using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class UpdateCoachProfileRequest
    {
        public string Bio { get; set; } = null!;
        public int ExperienceYears { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
