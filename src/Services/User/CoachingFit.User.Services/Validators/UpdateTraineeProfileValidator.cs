using CoachingFit.User.Shared.DTOs.Requests;
using FluentValidation;

namespace CoachingFit.User.Services.Validators
{
    public class UpdateTraineeProfileValidator : AbstractValidator<UpdateTraineeProfileRequest>
    {
        public UpdateTraineeProfileValidator()
        {
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
