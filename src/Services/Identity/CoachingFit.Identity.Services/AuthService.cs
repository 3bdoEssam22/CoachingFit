using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Core.Enums;
using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Messages;
using CoachingFit.Identity.Shared.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CoachingFit.Identity.Services
{
    public class AuthService(
        UserManager<ApplicationUser> _userManager,
        IJwtService _jwtService,
        IEmailService _emailService,
        ILogger<AuthService> _logger,
        IValidator<RegisterCoachRequest> _registerCoachValidator,
        IValidator<RegisterTraineeRequest> _registerTraineeValidator,
        IValidator<LoginRequest> _loginValidator) : IAuthService
    {
        public async Task<GenericResponse<AuthResponse>> RegisterCoachAsync(
            RegisterCoachRequest request, string baseUrl)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _registerCoachValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", result.Errors.Select(e => e.Description));
                return response;
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, nameof(UserRole.Coach));
            if (!addToRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = string.Join(" | ", addToRoleResult.Errors.Select(e => e.Description));
                return response;
            }

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl);

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = emailSent
                ? "Coach registered successfully. Please confirm your email."
                : "Coach registered successfully, but we couldn't send the confirmation email. Please use the resend option.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = nameof(UserRole.Coach),
                IsActive = false
            };
            return response;
        }
        public async Task<GenericResponse<AuthResponse>> RegisterTraineeAsync(RegisterTraineeRequest request, string baseUrl)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _registerTraineeValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", result.Errors.Select(e => e.Description));
                return response;
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, nameof(UserRole.Trainee));
            if (!addToRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = string.Join(" | ", addToRoleResult.Errors.Select(e => e.Description));
                return response;
            }

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl);

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = emailSent
                ? "Trainee registered successfully. Please confirm your email."
                : "Trainee registered successfully, but we couldn't send the confirmation email. Please use the resend option.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = nameof(UserRole.Trainee),
                IsActive = true
            };
            return response;
        }

        public async Task<GenericResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _loginValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid email or password.";
                return response;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "Account temporarily locked. Try again later.";
                return response;
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                await _userManager.AccessFailedAsync(user);
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid email or password.";
                return response;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Please confirm your email before logging in.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? nameof(UserRole.Trainee);
            var (token, expiresAt) = _jwtService.GenerateToken(user, role);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Login successful.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role,
                IsActive = user.IsActive,
                Token = token,
                ExpiresAt = expiresAt
            };
            return response;
        }

        public async Task<GenericResponse<AuthResponse>> GetCurrentUserAsync(string userId)
        {
            var response = new GenericResponse<AuthResponse>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "User not found.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? nameof(UserRole.Trainee);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "User retrieved successfully.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role,
                IsActive = user.IsActive
            };
            return response;
        }

        public async Task<GenericResponse<bool>> ConfirmEmailAsync(string userId, string token)
        {
            var response = new GenericResponse<bool>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Invalid or expired confirmation link.";
                return response;
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Email is already confirmed.";
                return response;
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Invalid or expired confirmation link.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Email confirmed successfully. You can now log in.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<bool>> ResendConfirmationEmailAsync(string email, string baseUrl)
        {
            var response = new GenericResponse<bool>();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || await _userManager.IsEmailConfirmedAsync(user))
            {
                // Return success regardless to prevent email enumeration
                response.StatusCode = StatusCodes.Status200OK;
                response.Message = "If your account exists and email is unconfirmed, you will receive a confirmation email shortly.";
                response.Data = true;
                return response;
            }

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl);
            if (!emailSent)
                _logger.LogWarning("ResendConfirmation: email delivery failed for {Email}", user.Email);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "If your account exists and email is unconfirmed, you will receive a confirmation email shortly.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<bool>> ActivateCoachAsync(string coachId)
        {
            var response = new GenericResponse<bool>();

            var user = await _userManager.FindByIdAsync(coachId);
            if (user is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach not found.";
                return response;
            }

            if (!await _userManager.IsInRoleAsync(user, nameof(UserRole.Coach)))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "User is not a coach.";
                return response;
            }

            if (user.IsActive)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Coach is already active.";
                return response;
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = string.Join(" | ", result.Errors.Select(e => e.Description));
                return response;
            }

            try
            {
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = user.Email!,
                    Subject = "CoachingFit — Account Activated!",
                    Body = $"""
                      <h2>Great news, {user.FirstName}!</h2>
                      <p>Your CoachingFit coach account has been approved and activated.</p>
                      <p>You can now log in and start building your coaching profile.</p>
                      <br/>
                      <p>The CoachingFit Team</p>
                      """
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send activation email to coach {CoachId}", coachId);
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach activated successfully.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<IEnumerable<string>>> GetPendingCoachUserIdsAsync()
        {
            var response = new GenericResponse<IEnumerable<string>>();

            var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
            var pendingIds = coaches
                .Where(c => !c.IsActive)
                .Select(c => c.Id);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Pending coach IDs retrieved successfully.";
            response.Data = pendingIds;
            return response;
        }

        // ── Private Helpers ────────────────────────────────────────────────────────
        private async Task<bool> TrySendConfirmationEmailAsync(ApplicationUser user, string baseUrl)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encoded = Uri.EscapeDataString(token);
                var link = $"{baseUrl}/api/Auth/confirm-email?userId={user.Id}&token={encoded}";

                await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = user.Email!,
                    Subject = "CoachingFit — Confirm Your Email",
                    Body = $"""
                      <h2>Welcome to CoachingFit, {user.FirstName}!</h2>
                      <p>Please confirm your email address by clicking the button below:</p>
                      <a href="{link}"
                         style="background:#1F4E79; color:white; padding:12px 24px;
                                text-decoration:none; border-radius:6px;">
                         Confirm Email
                      </a>
                      <p>If you didn't create an account, ignore this email.</p>
                      <p>This link expires in 24 hours.</p>
                      """
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                return false;
            }
        }

    }
}
