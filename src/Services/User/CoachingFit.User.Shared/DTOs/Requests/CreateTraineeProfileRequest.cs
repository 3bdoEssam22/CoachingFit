using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class CreateTraineeProfileRequest
    {
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public FitnessLevel FitnessLevel { get; set; }
        public string Goals { get; set; } = null!;
        public string? MedicalNotes { get; set; }
    }
}
