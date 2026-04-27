using CoachingFit.User.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class CreateTraineeProfileRequest
    {
        public string Gender { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public FitnessLevel FitnessLevel { get; set; }
        public string Goals { get; set; } = null!;
        public string? MedicalNotes { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
