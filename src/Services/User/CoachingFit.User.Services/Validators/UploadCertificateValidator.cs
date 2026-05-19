using CoachingFit.User.Shared.DTOs.Requests;
using FluentValidation;

namespace CoachingFit.User.Services.Validators
{
    public class UploadCertificateValidator : AbstractValidator<UploadCertificateRequest>
    {
        private static readonly string[] _allowedTypes =
            ["application/pdf", "image/jpeg", "image/jpg", "image/png", "image/webp"];

        public UploadCertificateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.IssuingOrganization)
                .NotEmpty().WithMessage("Issuing organization is required.")
                .MaximumLength(200).WithMessage("Issuing organization cannot exceed 200 characters.");

            RuleFor(x => x.IssuedDate)
                .NotEmpty().WithMessage("Issued date is required.")
                .Must(date => date <= DateTime.UtcNow).WithMessage("Issued date cannot be in the future.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Certificate file is required.");

            When(x => x.File is not null, () =>
            {
                RuleFor(x => x.File.Length)
                    .LessThanOrEqualTo(10 * 1024 * 1024)
                    .WithMessage("File must not exceed 10MB.");

                RuleFor(x => x.File.ContentType)
                    .Must(type => _allowedTypes.Contains(type))
                    .WithMessage("File must be a PDF, JPG, PNG, or WebP.");
            });
        }
    }
}
