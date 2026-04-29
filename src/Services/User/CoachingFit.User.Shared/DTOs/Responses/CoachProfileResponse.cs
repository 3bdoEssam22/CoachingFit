namespace CoachingFit.User.Shared.DTOs.Responses
{
    public class CoachProfileResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Bio { get; set; } = null!;
        public int ExperienceYears { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
