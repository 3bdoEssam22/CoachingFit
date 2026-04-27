using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class CreateCoachProfileRequest
    {
        public string Gender { get; set; } = null!;
        public string Bio { get; set; } = null!;
        public int ExperienceYears { get; set; }
        public IFormFile? Photo { get; set; }

    }
}
