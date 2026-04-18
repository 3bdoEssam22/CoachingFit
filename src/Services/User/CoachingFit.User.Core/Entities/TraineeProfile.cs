using CoachingFit.User.Core.Enums;
using System.Reflection;

namespace CoachingFit.User.Core.Entities
{
    public class TraineeProfile
    {
        public string UserId { get; set; } = null!;
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public FitnessLevel FitnessLevel { get; set; }
        public string Goals { get; set; } = null!;
        public string? MedicalNotes { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
}
