using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoachingFit.Identity.API.Controllers
{
    public class AuthController(IAuthService _authService) : BaseApiController
    {
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
            var result = await _authService.RegisterTraineeAsync(request);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();  

            var result = await _authService.GetCurrentUserAsync(userId);
            return HandleResponse(result);
        }
    }
}
