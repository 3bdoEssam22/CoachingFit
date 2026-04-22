using CoachingFit.User.Shared.DTOs.Requests;
using FluentValidation;

namespace CoachingFit.User.Services.Validators
{
    public class UpdateCoachProfileValidator : AbstractValidator<UpdateCoachProfileRequest>
    {
        public UpdateCoachProfileValidator()
        {
            RuleFor(x => x.Bio)
                .NotEmpty().WithMessage("Bio is required.")
                .MaximumLength(1000).WithMessage("Bio cannot exceed 1000 characters.");

            RuleFor(x => x.ExperienceYears)
                .GreaterThanOrEqualTo(0).WithMessage("Experience years cannot be negative.")
                .LessThanOrEqualTo(50).WithMessage("Experience years cannot exceed 50.");
        }
    }
}
