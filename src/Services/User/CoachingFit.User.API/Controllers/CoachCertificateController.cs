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
    public class CoachCertificateController(ICoachCertificateService _certificateService)
            : BaseApiController
    {
        // POST api/CoachCertificate
        [Idempotent]
        [Authorize(Roles = "Coach")]
        [HttpPost]
        public async Task<ActionResult<GenericResponse<CertificateResponse>>> Upload(
            [FromForm] UploadCertificateRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.UploadAsync(request, userId, ct);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/me
        [Authorize(Roles = "Coach")]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<IEnumerable<CertificateResponse>>>> GetMy(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.GetMyCertificatesAsync(userId, ct);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/{id}
        [Authorize(Roles = "Coach")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GenericResponse<CertificateResponse>>> GetById(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.GetByIdAsync(id, userId, ct);
            return HandleResponse(result);
        }

        // DELETE api/CoachCertificate/{id}
        [Authorize(Roles = "Coach")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<GenericResponse<bool>>> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.DeleteAsync(id, userId, ct);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<ActionResult<GenericResponse<IEnumerable<AdminCertificateResponse>>>> GetPending(CancellationToken ct)
        {
            var result = await _certificateService.GetPendingAsync(ct);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/coach/{userId}
        [Authorize(Roles = "Admin")]
        [HttpGet("coach/{coachUserId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<AdminCertificateResponse>>>> GetByCoach(
            string coachUserId, CancellationToken ct)
        {
            var result = await _certificateService.GetByCoachUserIdAsync(coachUserId, ct);
            return HandleResponse(result);
        }

        // PUT api/CoachCertificate/{id}/approve
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<ActionResult<GenericResponse<bool>>> Approve(Guid id, CancellationToken ct)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized();

            var result = await _certificateService.ApproveAsync(id, adminId, ct);
            return HandleResponse(result);
        }

        // PUT api/CoachCertificate/{id}/reject
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reject")]
        public async Task<ActionResult<GenericResponse<bool>>> Reject(
            Guid id, [FromBody] RejectCertificateRequest request, CancellationToken ct)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized();

            var result = await _certificateService.RejectAsync(id, adminId, request, ct);
            return HandleResponse(result);
        }
    }
}
