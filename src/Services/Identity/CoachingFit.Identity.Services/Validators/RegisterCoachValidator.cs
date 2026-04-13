using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Shared.DTOs.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CoachingFit.Identity.Services.Validators
{
    public class RegisterCoachValidator : AbstractValidator<RegisterCoachRequest>
    {
        public RegisterCoachValidator(UserManager<ApplicationUser> userManager)
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Enter a valid email address.")
                .MustAsync(async (email, _) =>
                    await userManager.FindByEmailAsync(email) is null)
                .WithMessage("Email is already taken.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^01[0125][0-9]{8}$")
                .WithMessage("Enter a valid Egyptian phone number.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }
}
