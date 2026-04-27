using CoachingFit.User.Services.Abstraction;
using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoachingFit.User.API.Controllers
{
    public class TraineeProfileController(ITraineeProfileService _traineeProfileService)
            : BaseApiController
    {
        // POST api/TraineeProfile
        [Authorize(Roles = "Trainee")]
        [HttpPost]
        public async Task<ActionResult<GenericResponse<TraineeProfileResponse>>> Create(
            [FromForm] CreateTraineeProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _traineeProfileService.CreateAsync(request, userId);
            return HandleResponse(result);
        }

        // GET api/TraineeProfile/{id}
        [Authorize(Roles = "Coach")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GenericResponse<TraineeProfileResponse>>> GetById(Guid id)
        {
            var result = await _traineeProfileService.GetByIdAsync(id);
            return HandleResponse(result);
        }

        // GET api/TraineeProfile/me
        [Authorize(Roles = "Trainee")]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<TraineeProfileResponse>>> GetMy()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _traineeProfileService.GetByUserIdAsync(userId);
            return HandleResponse(result);
        }

        // PUT api/TraineeProfile
        [Authorize(Roles = "Trainee")]
        [HttpPut]
        public async Task<ActionResult<GenericResponse<TraineeProfileResponse>>> Update(
            [FromForm] UpdateTraineeProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _traineeProfileService.UpdateAsync(request, userId);
            return HandleResponse(result);
        }
    }
}
