using CoachingFit.User.API.Infrastructure.Idempotency;
using CoachingFit.User.Services.Abstraction;
using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoachingFit.User.API.Controllers
{
    public class CoachProfileController(ICoachProfileService _coachProfileService)
            : BaseApiController
    {
        // POST api/CoachProfile
        [Idempotent]
        [Authorize(Roles = "Coach")]
        [HttpPost]
        public async Task<ActionResult<GenericResponse<CoachProfileResponse>>> Create(
            [FromForm] CreateCoachProfileRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _coachProfileService.CreateAsync(request, userId, ct);
            return HandleResponse(result);
        }

        // GET api/CoachProfile/{id}
        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GenericResponse<CoachProfileResponse>>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _coachProfileService.GetByIdAsync(id, ct);
            return HandleResponse(result);
        }

        // GET api/CoachProfile/me
        [Authorize(Roles = "Coach")]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<CoachProfileResponse>>> GetMy(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _coachProfileService.GetByUserIdAsync(userId, ct);
            return HandleResponse(result);
        }

        // PUT api/CoachProfile
        [Authorize(Roles = "Coach")]
        [HttpPut]
        public async Task<ActionResult<GenericResponse<CoachProfileResponse>>> Update(
            [FromForm] UpdateCoachProfileRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _coachProfileService.UpdateAsync(request, userId, ct);
            return HandleResponse(result);
        }

        // GET api/CoachProfile/pending?userIds=id1&userIds=id2
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<ActionResult<GenericResponse<IEnumerable<CoachProfileResponse>>>> GetPending(
            [FromQuery] List<string> userIds, CancellationToken ct)
        {
            var result = await _coachProfileService.GetAllPendingAsync(userIds, ct);
            return HandleResponse(result);
        }
    }
}
