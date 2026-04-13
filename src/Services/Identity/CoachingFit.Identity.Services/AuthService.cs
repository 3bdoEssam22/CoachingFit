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

namespace CoachingFit.Identity.Services
{
    public class AuthService(
        UserManager<ApplicationUser> _userManager,
        IJwtService _jwtService,
        IEmailService _emailService,
        IValidator<RegisterCoachRequest> _registerCoachValidator,
        IValidator<RegisterTraineeRequest> _registerTraineeValidator,
        IValidator<LoginRequest> _loginValidator) : IAuthService
    {
        public async Task<GenericResponse<AuthResponse>> RegisterCoachAsync(RegisterCoachRequest request)
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

            await _userManager.AddToRoleAsync(user, nameof(UserRole.Coach));

            // Confirm coach email automatically
            // (coach doesn't need email confirmation — admin verifies credentials instead)
            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _userManager.ConfirmEmailAsync(user, confirmToken);

            // Send application received email
            await _emailService.SendEmailAsync(new EmailMessage
            {
                To = user.Email!,
                Subject = "CoachingFit — Application Received",
                Body = $"""
                          <h2>Hi {user.FirstName},</h2>
                          <p>Thank you for applying to join CoachingFit as a coach!</p>
                          <p>Your application is currently under review. Our team will verify 
                          your credentials and activate your account shortly.</p>
                          <p>You will receive another email once your account is activated.</p>
                          <br/>
                          <p>The CoachingFit Team</p>
                          """
            });

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach registered successfully. Waiting for admin approval.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = nameof(UserRole.Coach)
            };
            return response;
        }

        public async Task<GenericResponse<AuthResponse>> RegisterTraineeAsync(RegisterTraineeRequest request)
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

            await _userManager.AddToRoleAsync(user, nameof(UserRole.Trainee));

            var (token, expiresAt) = _jwtService.GenerateToken(user, nameof(UserRole.Trainee));


            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Trainee registered successfully.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = nameof(UserRole.Trainee),
                Token = token,
                ExpiresAt = expiresAt
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

            if (!user.IsActive)
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "Your account is not active.";
                return response;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "Your account is temporarily locked due to multiple failed login attempts. Please try again later.";
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

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            var (token, expiresAt) = _jwtService.GenerateToken(user, role);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Login successful.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role,
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
            var role = roles.FirstOrDefault() ?? string.Empty;

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "User retrieved successfully.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role
            };
            return response;
        }
    }
}
