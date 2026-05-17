using CoachingFit.Identity.API.Infrastructure.Idempotency;
using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CoachingFit.Identity.API.Controllers
{
    public class AuthController(IAuthService _authService, IConfiguration _config) : BaseApiController
    {
        // POST api/Auth/register/coach
        [Idempotent]
        [EnableRateLimiting("auth-limit")]
        [HttpPost("register/coach")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterCoach(
            [FromBody] RegisterCoachRequest request, CancellationToken ct)
        {
            var result = await _authService.RegisterCoachAsync(request, _config["App:BaseUrl"]!, ct);
            return HandleResponse(result);
        }

        // POST api/Auth/register/trainee
        [Idempotent]
        [EnableRateLimiting("auth-limit")]
        [HttpPost("register/trainee")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterTrainee(
            [FromBody] RegisterTraineeRequest request, CancellationToken ct)
        {
            var result = await _authService.RegisterTraineeAsync(request, _config["App:BaseUrl"]!, ct);
            return HandleResponse(result);
        }

        // POST api/Auth/login
        [EnableRateLimiting("auth-limit")]
        [HttpPost("login")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> Login(
            [FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);
            return HandleResponse(result);
        }

        // GET api/Auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> Me(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _authService.GetCurrentUserAsync(userId, ct);
            return HandleResponse(result);
        }

        // GET api/Auth/confirm-email?userId=&token=
        [HttpGet("confirm-email")]
        public async Task<ActionResult<GenericResponse<bool>>> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token,
            CancellationToken ct)
        {
            var result = await _authService.ConfirmEmailAsync(userId, token, ct);
            return HandleResponse(result);
        }

        // POST api/Auth/resend-confirmation?email=
        [EnableRateLimiting("auth-limit")]
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<GenericResponse<bool>>> ResendConfirmation(
            [FromQuery] string email, CancellationToken ct)
        {
            var result = await _authService.ResendConfirmationEmailAsync(email, _config["App:BaseUrl"]!, ct);
            return HandleResponse(result);
        }

        // POST api/Auth/refresh
        [Idempotent]
        [EnableRateLimiting("auth-limit")]
        [HttpPost("refresh")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> Refresh(
            [FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
            return HandleResponse(result);
        }

        // POST api/Auth/revoke
        [EnableRateLimiting("auth-limit")]
        [HttpPost("revoke")]
        public async Task<ActionResult<GenericResponse<bool>>> Revoke(
            [FromBody] RevokeTokenRequest request, CancellationToken ct)
        {
            var result = await _authService.RevokeTokenAsync(request.RefreshToken, ct);
            return HandleResponse(result);
        }

        // PUT api/Auth/coaches/{id}/activate
        [Authorize(Roles = "Admin")]
        [HttpPut("coaches/{id}/activate")]
        public async Task<ActionResult<GenericResponse<bool>>> ActivateCoach(string id, CancellationToken ct)
        {
            var result = await _authService.ActivateCoachAsync(id, ct);
            return HandleResponse(result);
        }

        // GET api/Auth/coaches/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("coaches/pending")]
        public async Task<ActionResult<GenericResponse<IEnumerable<string>>>> GetPendingCoaches(CancellationToken ct)
        {
            var result = await _authService.GetPendingCoachUserIdsAsync(ct);
            return HandleResponse(result);
        }
    }
}
