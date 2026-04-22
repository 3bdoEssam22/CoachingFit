using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Core.Entities
{
    public class CoachProfile : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public string Bio { get; set; } = null!;
        public int ExperienceYears { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public Gender Gender { get; set; }

    }
}
