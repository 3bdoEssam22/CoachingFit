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
        IValidator<LoginRequest> _loginValidator,
        TimeProvider _timeProvider,
        IRefreshTokenService _refreshTokens) : IAuthService
    {
        public async Task<GenericResponse<AuthResponse>> RegisterCoachAsync(
            RegisterCoachRequest request, string baseUrl, CancellationToken ct = default)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _registerCoachValidator.ValidateAsync(request, ct);
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

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl, ct);

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
        public async Task<GenericResponse<AuthResponse>> RegisterTraineeAsync(RegisterTraineeRequest request, string baseUrl, CancellationToken ct = default)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _registerTraineeValidator.ValidateAsync(request, ct);
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

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl, ct);

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

        public async Task<GenericResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var response = new GenericResponse<AuthResponse>();

            var validation = await _loginValidator.ValidateAsync(request, ct);
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
            var (refreshPlaintext, refreshEntity) = await _refreshTokens.IssueAsync(user.Id, ct);

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
                ExpiresAt = expiresAt,
                RefreshToken = refreshPlaintext,
                RefreshTokenExpiresAt = refreshEntity.ExpiresAt
            };
            return response;
        }

        public async Task<GenericResponse<AuthResponse>> GetCurrentUserAsync(string userId, CancellationToken ct = default)
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

        public async Task<GenericResponse<bool>> ConfirmEmailAsync(string userId, string token, CancellationToken ct = default)
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

        public async Task<GenericResponse<bool>> ResendConfirmationEmailAsync(string email, string baseUrl, CancellationToken ct = default)
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

            var emailSent = await TrySendConfirmationEmailAsync(user, baseUrl, ct);
            if (!emailSent)
                _logger.LogWarning("ResendConfirmation: email delivery failed for user {UserId}", user.Id);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "If your account exists and email is unconfirmed, you will receive a confirmation email shortly.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<bool>> ActivateCoachAsync(string coachId, CancellationToken ct = default)
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
            user.RejectionReason = null;
            user.RejectedAt = null;
            user.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

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
                }, ct);
            }
            catch (Exception ex) when (ex is MailKit.ServiceNotConnectedException
                                          or MailKit.ServiceNotAuthenticatedException
                                          or MailKit.Security.AuthenticationException
                                          or MailKit.Net.Smtp.SmtpCommandException
                                          or MailKit.Net.Smtp.SmtpProtocolException
                                          or System.Net.Sockets.SocketException
                                          or System.IO.IOException
                                          or TimeoutException
                                          or MimeKit.ParseException)
            {
                _logger.LogError(ex, "Failed to send activation email to coach {CoachId}", coachId);
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach activated successfully.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<IEnumerable<string>>> GetPendingCoachUserIdsAsync(CancellationToken ct = default)
        {
            var response = new GenericResponse<IEnumerable<string>>();

            var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
            // Pending = inactive AND not previously rejected. Rejected coaches stay out of the
            // pending queue until they re-apply (which clears RejectionReason via re-upload).
            var pendingIds = coaches
                .Where(c => !c.IsActive && c.RejectionReason == null)
                .Select(c => c.Id);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Pending coach IDs retrieved successfully.";
            response.Data = pendingIds;
            return response;
        }

        public async Task<GenericResponse<AuthResponse>> RefreshTokenAsync(
            string plaintextRefreshToken, CancellationToken ct = default)
        {
            var response = new GenericResponse<AuthResponse>();

            if (string.IsNullOrWhiteSpace(plaintextRefreshToken))
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid refresh token.";
                return response;
            }

            var stored = await _refreshTokens.FindByPlaintextAsync(plaintextRefreshToken, ct);
            if (stored is null)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid refresh token.";
                return response;
            }

            if (stored.RevokedAt is not null)
            {
                _logger.LogWarning("Refresh-token reuse detected for user {UserId}", stored.UserId);
                await _refreshTokens.RevokeAllForUserAsync(stored.UserId, "reuse-detected", ct);
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid refresh token.";
                return response;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (now >= stored.ExpiresAt || now >= stored.AbsoluteExpiresAt)
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Refresh token expired.";
                return response;
            }

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Invalid refresh token.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? nameof(UserRole.Trainee);
            var (token, expiresAt) = _jwtService.GenerateToken(user, role);
            var (newPlaintext, newEntity) = await _refreshTokens.RotateAsync(stored, ct);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Token refreshed successfully.";
            response.Data = new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role,
                IsActive = user.IsActive,
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = newPlaintext,
                RefreshTokenExpiresAt = newEntity.ExpiresAt
            };
            return response;
        }

        public async Task<GenericResponse<bool>> RevokeTokenAsync(
            string plaintextRefreshToken, CancellationToken ct = default)
        {
            var response = new GenericResponse<bool>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Token revoked.",
                Data = true
            };

            if (string.IsNullOrWhiteSpace(plaintextRefreshToken))
                return response;

            var stored = await _refreshTokens.FindByPlaintextAsync(plaintextRefreshToken, ct);
            if (stored is null || stored.RevokedAt is not null)
                return response;

            await _refreshTokens.RevokeAsync(stored, "logout", ct);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<string>>> GetAllCoachUserIdsAsync(CancellationToken ct = default)
        {
            var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
            return new GenericResponse<IEnumerable<string>>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "All coach IDs retrieved successfully.",
                Data = coaches.Select(c => c.Id)
            };
        }

        public async Task<GenericResponse<IEnumerable<string>>> GetAllTraineeUserIdsAsync(CancellationToken ct = default)
        {
            var trainees = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Trainee));
            return new GenericResponse<IEnumerable<string>>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "All trainee IDs retrieved successfully.",
                Data = trainees.Select(t => t.Id)
            };
        }

        public async Task<GenericResponse<AdminStatsResponse>> GetStatsAsync(CancellationToken ct = default)
        {
            // Must run sequentially — UserManager shares the scoped DbContext and EF Core
            // forbids concurrent operations on a single context instance.
            var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
            var trainees = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Trainee));

            return new GenericResponse<AdminStatsResponse>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Stats retrieved successfully.",
                Data = new AdminStatsResponse
                {
                    TotalCoaches = coaches.Count,
                    ActiveCoaches = coaches.Count(c => c.IsActive),
                    // Pending = inactive AND not previously rejected. Matches the same
                    // definition used by GetPendingCoachUserIdsAsync so the card count
                    // always matches the /coaches/pending list length.
                    PendingCoaches = coaches.Count(c => !c.IsActive && c.RejectionReason == null),
                    TotalTrainees = trainees.Count
                }
            };
        }

        public async Task<GenericResponse<IEnumerable<CoachUserSummary>>> GetCoachDetailsAsync(CancellationToken ct = default)
        {
            var coaches = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Coach));
            var summaries = coaches.Select(c => new CoachUserSummary
            {
                UserId = c.Id,
                FullName = $"{c.FirstName} {c.LastName}".Trim(),
                Email = c.Email ?? string.Empty,
                IsActive = c.IsActive,
                RejectionReason = c.RejectionReason,
                RejectedAt = c.RejectedAt
            });

            return new GenericResponse<IEnumerable<CoachUserSummary>>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Coach details retrieved successfully.",
                Data = summaries
            };
        }

        public async Task<GenericResponse<bool>> RejectCoachAsync(string coachId, string reason, CancellationToken ct = default)
        {
            var response = new GenericResponse<bool>();

            if (string.IsNullOrWhiteSpace(reason))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Rejection reason is required.";
                return response;
            }

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
                response.Message = "Coach is already active and cannot be rejected. Deactivate first.";
                return response;
            }

            user.RejectionReason = reason;
            user.RejectedAt = _timeProvider.GetUtcNow().UtcDateTime;
            user.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            var persisted = await _userManager.UpdateAsync(user);
            if (!persisted.Succeeded)
            {
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = string.Join(" | ", persisted.Errors.Select(e => e.Description));
                return response;
            }

            try
            {
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = user.Email!,
                    Subject = "CoachingFit — Application Update",
                    Body = $"""
                      <h2>Hi {user.FirstName},</h2>
                      <p>We've reviewed your coach application and we're unable to approve it at this time.</p>
                      <p><strong>Reason:</strong></p>
                      <blockquote style="border-left: 3px solid #ccc; padding-left: 12px; color: #555;">
                        {System.Net.WebUtility.HtmlEncode(reason)}
                      </blockquote>
                      <p>You can update your profile and upload new certificates from the app, and we'll review again.</p>
                      <br/>
                      <p>The CoachingFit Team</p>
                      """
                }, ct);
            }
            catch (Exception ex) when (ex is MailKit.ServiceNotConnectedException
                                          or MailKit.ServiceNotAuthenticatedException
                                          or MailKit.Security.AuthenticationException
                                          or MailKit.Net.Smtp.SmtpCommandException
                                          or MailKit.Net.Smtp.SmtpProtocolException
                                          or System.Net.Sockets.SocketException
                                          or System.IO.IOException
                                          or TimeoutException
                                          or MimeKit.ParseException)
            {
                _logger.LogError(ex, "Failed to send rejection email to coach {CoachId}", coachId);
                // Rejection is persisted; email failure should not undo it. Log and continue.
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach rejected.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<CoachUserSummary>> GetCoachSummaryAsync(string coachId, CancellationToken ct = default)
        {
            var response = new GenericResponse<CoachUserSummary>();

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

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach summary retrieved.";
            response.Data = new CoachUserSummary
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                RejectionReason = user.RejectionReason,
                RejectedAt = user.RejectedAt
            };
            return response;
        }

        public async Task<GenericResponse<bool>> DeactivateCoachAsync(string coachId, string reason, CancellationToken ct = default)
        {
            var response = new GenericResponse<bool>();

            if (string.IsNullOrWhiteSpace(reason))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Deactivation reason is required.";
                return response;
            }

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

            if (!user.IsActive)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Coach is already inactive.";
                return response;
            }

            user.IsActive = false;
            user.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

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
                    Subject = "CoachingFit — Account Deactivated",
                    Body = $"""
                      <h2>Hi {user.FirstName},</h2>
                      <p>Your CoachingFit coach account has been deactivated by an administrator. You will not be able to log in until the account is reactivated.</p>
                      <p><strong>Reason:</strong></p>
                      <blockquote style="border-left: 3px solid #ccc; padding-left: 12px; color: #555;">
                        {System.Net.WebUtility.HtmlEncode(reason)}
                      </blockquote>
                      <p>If you believe this was a mistake, please contact our support team.</p>
                      <br/>
                      <p>The CoachingFit Team</p>
                      """
                }, ct);
            }
            catch (Exception ex) when (ex is MailKit.ServiceNotConnectedException
                                          or MailKit.ServiceNotAuthenticatedException
                                          or MailKit.Security.AuthenticationException
                                          or MailKit.Net.Smtp.SmtpCommandException
                                          or MailKit.Net.Smtp.SmtpProtocolException
                                          or System.Net.Sockets.SocketException
                                          or System.IO.IOException
                                          or TimeoutException
                                          or MimeKit.ParseException)
            {
                _logger.LogError(ex, "Failed to send deactivation email to coach {CoachId}", coachId);
                // Don't fail the whole operation — the deactivation already succeeded.
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach deactivated successfully.";
            response.Data = true;
            return response;
        }

        // ── Private Helpers ────────────────────────────────────────────────────────
        private async Task<bool> TrySendConfirmationEmailAsync(ApplicationUser user, string baseUrl, CancellationToken ct = default)
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
                }, ct);
                return true;
            }
            catch (Exception ex) when (ex is MailKit.ServiceNotConnectedException
                                          or MailKit.ServiceNotAuthenticatedException
                                          or MailKit.Security.AuthenticationException
                                          or MailKit.Net.Smtp.SmtpCommandException
                                          or MailKit.Net.Smtp.SmtpProtocolException
                                          or System.Net.Sockets.SocketException
                                          or System.IO.IOException
                                          or TimeoutException
                                          or MimeKit.ParseException)
            {
                _logger.LogError(ex, "Failed to send confirmation email for user {UserId}", user.Id);
                return false;
            }
        }

    }
}
