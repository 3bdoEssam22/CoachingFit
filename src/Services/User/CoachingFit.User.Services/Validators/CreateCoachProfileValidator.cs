using CoachingFit.User.Shared.DTOs.Requests;
using FluentValidation;
using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Services.Validators
{
    public class CreateCoachProfileValidator : AbstractValidator<CreateCoachProfileRequest>
    {
        private static readonly string[] _allowedTypes =
            ["image/jpeg", "image/jpg", "image/png", "image/webp"];
        public CreateCoachProfileValidator()
        {
            RuleFor(x => x.Bio)
                .NotEmpty().WithMessage("Bio is required.")
                .MaximumLength(1000).WithMessage("Bio cannot exceed 1000 characters.");

            RuleFor(x => x.ExperienceYears)
                .GreaterThanOrEqualTo(0).WithMessage("Experience years cannot be negative.")
                .LessThanOrEqualTo(50).WithMessage("Experience years cannot exceed 50.");

            RuleFor(x => x.Gender)
                .Must(g => Enum.TryParse<Gender>(g, true, out _))
                .WithMessage("Gender must be Male or Female.");

            When(x => x.Photo is not null, () =>
            {
                RuleFor(x => x.Photo!.Length)
                    .LessThanOrEqualTo(5 * 1024 * 1024)
                    .WithMessage("Photo must not exceed 5MB.");

                RuleFor(x => x.Photo!.ContentType)
                    .Must(type => _allowedTypes.Contains(type))
                    .WithMessage("Photo must be a JPG, PNG, or WebP image.");
            });
        }
    }
}
