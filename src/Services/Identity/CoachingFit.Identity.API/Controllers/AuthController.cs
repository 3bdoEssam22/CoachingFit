using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CoachingFit.Identity.API.Controllers
{
    public class AuthController(IAuthService _authService) : BaseApiController
    {
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}";


        // POST api/Auth/register/coach
        [HttpPost("register/coach")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterCoach(
            [FromBody] RegisterCoachRequest request)
        {
            var result = await _authService.RegisterCoachAsync(request);
            return HandleResponse(result);
        }

        // POST api/Auth/register/trainee
        [HttpPost("register/trainee")]
        public async Task<ActionResult<GenericResponse<AuthResponse>>> RegisterTrainee(
            [FromBody] RegisterTraineeRequest request)
        {
            var result = await _authService.RegisterTraineeAsync(request, GetBaseUrl());
            return HandleResponse(result);
        }

        // POST api/Auth/login
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
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
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
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<GenericResponse<bool>>> ResendConfirmation(
            [FromQuery] string email)
        {
            var result = await _authService.ResendConfirmationEmailAsync(email, GetBaseUrl());
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
    }
}
