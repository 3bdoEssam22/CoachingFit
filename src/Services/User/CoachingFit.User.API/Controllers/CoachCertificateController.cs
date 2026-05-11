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
        [Authorize(Roles = "Coach")]
        [HttpPost]
        public async Task<ActionResult<GenericResponse<CertificateResponse>>> Upload(
            [FromForm] UploadCertificateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.UploadAsync(request, userId);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/me
        [Authorize(Roles = "Coach")]
        [HttpGet("me")]
        public async Task<ActionResult<GenericResponse<IEnumerable<CertificateResponse>>>> GetMy()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.GetMyCertificatesAsync(userId);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/{id}
        [Authorize(Roles = "Coach")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GenericResponse<CertificateResponse>>> GetById(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.GetByIdAsync(id, userId);
            return HandleResponse(result);
        }

        // DELETE api/CoachCertificate/{id}
        [Authorize(Roles = "Coach")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<GenericResponse<bool>>> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _certificateService.DeleteAsync(id, userId);
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<ActionResult<GenericResponse<IEnumerable<AdminCertificateResponse>>>> GetPending()
        {
            var result = await _certificateService.GetPendingAsync();
            return HandleResponse(result);
        }

        // GET api/CoachCertificate/coach/{userId}
        [Authorize(Roles = "Admin")]
        [HttpGet("coach/{coachUserId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<AdminCertificateResponse>>>> GetByCoach(
            string coachUserId)
        {
            var result = await _certificateService.GetByCoachUserIdAsync(coachUserId);
            return HandleResponse(result);
        }

        // PUT api/CoachCertificate/{id}/approve
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<ActionResult<GenericResponse<bool>>> Approve(Guid id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized();

            var result = await _certificateService.ApproveAsync(id, adminId);
            return HandleResponse(result);
        }

        // PUT api/CoachCertificate/{id}/reject
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reject")]
        public async Task<ActionResult<GenericResponse<bool>>> Reject(
            Guid id, [FromBody] RejectCertificateRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
                return Unauthorized();

            var result = await _certificateService.RejectAsync(id, adminId, request);
            return HandleResponse(result);
        }
    }
}
