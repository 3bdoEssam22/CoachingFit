using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class UpdateTraineeProfileRequest
    {
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public FitnessLevel FitnessLevel { get; set; }
        public string Goals { get; set; } = null!;
        public string? MedicalNotes { get; set; }
    }
}
