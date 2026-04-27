using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Shared.DTOs.Responses
{
    public class TraineeProfileResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public FitnessLevel FitnessLevel { get; set; }
        public string Goals { get; set; } = null!;
        public string? MedicalNotes { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
