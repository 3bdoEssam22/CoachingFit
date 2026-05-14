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
        [EnableRateLimiting("auth-limit")]
        [HttpPost("register/coach")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterCoach(
            [FromBody] RegisterCoachRequest request)
        {
            var result = await _authService.RegisterCoachAsync(request, _config["App:BaseUrl"]!);
            return HandleResponse(result);
        }

        // POST api/Auth/register/trainee
        [EnableRateLimiting("auth-limit")]
        [HttpPost("register/trainee")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterTrainee(
            [FromBody] RegisterTraineeRequest request)
        {
            var result = await _authService.RegisterTraineeAsync(request, _config["App:BaseUrl"]!);
            return HandleResponse(result);
        }

        // POST api/Auth/login
        [EnableRateLimiting("auth-limit")]
        [HttpPost("login")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> Login(
            [FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return HandleResponse(result);
        }

        // GET api/Auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _authService.GetCurrentUserAsync(userId);
            return HandleResponse(result);
        }

        // GET api/Auth/confirm-email?userId=&token=
        [HttpGet("confirm-email")]
        public async Task<ActionResult<GenericResponse<bool>>> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailAsync(userId, token);
            return HandleResponse(result);
        }

        // POST api/Auth/resend-confirmation?email=
        [EnableRateLimiting("auth-limit")]
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<GenericResponse<bool>>> ResendConfirmation(
            [FromQuery] string email)
        {
            var result = await _authService.ResendConfirmationEmailAsync(email, _config["App:BaseUrl"]!);
            return HandleResponse(result);
        }

        // PUT api/Auth/coaches/{id}/activate
        [Authorize(Roles = "Admin")]
        [HttpPut("coaches/{id}/activate")]
        public async Task<ActionResult<GenericResponse<bool>>> ActivateCoach(string id)
        {
            var result = await _authService.ActivateCoachAsync(id);
            return HandleResponse(result);
        }

        // GET api/Auth/coaches/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("coaches/pending")]
        public async Task<ActionResult<GenericResponse<IEnumerable<string>>>> GetPendingCoaches()
        {
            var result = await _authService.GetPendingCoachUserIdsAsync();
            return HandleResponse(result);
        }
    }
}
