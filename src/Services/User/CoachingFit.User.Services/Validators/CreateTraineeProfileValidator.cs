using CoachingFit.User.Shared.DTOs.Requests;
using FluentValidation;

namespace CoachingFit.User.Services.Validators
{
    public class CreateTraineeProfileValidator : AbstractValidator<CreateTraineeProfileRequest>
    {
        public CreateTraineeProfileValidator()
        {
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("Invalid gender value.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.")
                .LessThan(DateTime.UtcNow.AddYears(-10)).WithMessage("Trainee must be at least 10 years old.")
                .GreaterThan(DateTime.UtcNow.AddYears(-100)).WithMessage("Invalid date of birth.");

            RuleFor(x => x.WeightKg)
                .GreaterThan(0).WithMessage("Weight must be greater than 0.")
                .LessThanOrEqualTo(500).WithMessage("Weight cannot exceed 500 kg.");

            RuleFor(x => x.HeightCm)
                .GreaterThan(0).WithMessage("Height must be greater than 0.")
                .LessThanOrEqualTo(300).WithMessage("Height cannot exceed 300 cm.");

            RuleFor(x => x.FitnessLevel)
                .IsInEnum().WithMessage("Invalid fitness level value.");

            RuleFor(x => x.Goals)
                .NotEmpty().WithMessage("Goals are required.")
                .MaximumLength(500).WithMessage("Goals cannot exceed 500 characters.");

            RuleFor(x => x.MedicalNotes)
                .MaximumLength(500).WithMessage("Medical notes cannot exceed 500 characters.")
                .When(x => x.MedicalNotes is not null);
        }
    }
}
