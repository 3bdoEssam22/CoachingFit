using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class CreateCoachProfileRequest
    {
        public Gender Gender { get; set; }
        public string Bio { get; set; } = null!;
        public int ExperienceYears { get; set; }
    }
}
