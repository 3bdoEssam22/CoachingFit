using CoachingFit.User.Core.Contracts;
using CoachingFit.User.Core.Entities;
using CoachingFit.User.Core.Enums;
using CoachingFit.User.Services.Abstraction;
using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachingFit.User.Services
{
    public class CoachCertificateService(
        IUserDbContext _context,
        ICloudinaryService _cloudinaryService,
        ILogger<CoachCertificateService> _logger,
        IValidator<UploadCertificateRequest> _uploadValidator,
        IValidator<RejectCertificateRequest> _rejectValidator) : ICoachCertificateService
    {
        public async Task<GenericResponse<CertificateResponse>> UploadAsync(
            UploadCertificateRequest request, string userId)
        {
            var response = new GenericResponse<CertificateResponse>();

            var validation = await _uploadValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Please create your coach profile before uploading certificates.";
                return response;
            }

            string fileUrl;
            string fileType;
            try
            {
                (fileUrl, fileType) = await _cloudinaryService.UploadCertificateAsync(request.File);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload certificate for coach {UserId}", userId);
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "Failed to upload certificate file. Please try again later.";
                return response;
            }

            var certificate = new CoachCertificate
            {
                Id = Guid.NewGuid(),
                CoachProfileId = profile.Id,
                Title = request.Title,
                IssuingOrganization = request.IssuingOrganization,
                IssuedDate = request.IssuedDate,
                FileUrl = fileUrl,
                FileName = request.File.FileName,
                FileType = fileType,
                Status = CertificateStatus.Pending
            };

            await _context.AddCoachCertificateAsync(certificate);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = "Certificate uploaded successfully. Admin will review it shortly.";
            response.Data = MapToResponse(certificate);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<CertificateResponse>>> GetMyCertificatesAsync(string userId)
        {
            var response = new GenericResponse<IEnumerable<CertificateResponse>>();

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            var certificates = await _context.CoachCertificates
                .Where(c => c.CoachProfileId == profile.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Certificates retrieved successfully.";
            response.Data = certificates.Select(MapToResponse);
            return response;
        }

        public async Task<GenericResponse<CertificateResponse>> GetByIdAsync(Guid id, string userId)
        {
            var response = new GenericResponse<CertificateResponse>();

            var certificate = await _context.CoachCertificates
                .Include(c => c.CoachProfile)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Certificate not found.";
                return response;
            }

            if (certificate.CoachProfile.UserId != userId)
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "You do not have access to this certificate.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Certificate retrieved successfully.";
            response.Data = MapToResponse(certificate);
            return response;
        }

        public async Task<GenericResponse<bool>> DeleteAsync(Guid id, string userId)
        {
            var response = new GenericResponse<bool>();

            var certificate = await _context.CoachCertificates
                .Include(c => c.CoachProfile)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Certificate not found.";
                return response;
            }

            if (certificate.CoachProfile.UserId != userId)
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "You do not have access to this certificate.";
                return response;
            }

            if (certificate.Status == CertificateStatus.Approved)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Approved certificates cannot be deleted.";
                return response;
            }

            _context.RemoveCoachCertificate(certificate);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Certificate deleted successfully.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetPendingAsync()
        {
            var response = new GenericResponse<IEnumerable<AdminCertificateResponse>>();

            var certificates = await _context.CoachCertificates
                .Include(c => c.CoachProfile)
                .Where(c => c.Status == CertificateStatus.Pending)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Pending certificates retrieved successfully.";
            response.Data = certificates.Select(MapToAdminResponse);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetByCoachUserIdAsync(string coachUserId)
        {
            var response = new GenericResponse<IEnumerable<AdminCertificateResponse>>();

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == coachUserId);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            var certificates = await _context.CoachCertificates
                .Include(c => c.CoachProfile)
                .Where(c => c.CoachProfileId == profile.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach certificates retrieved successfully.";
            response.Data = certificates.Select(MapToAdminResponse);
            return response;
        }

        public async Task<GenericResponse<bool>> ApproveAsync(Guid id, string adminId)
        {
            var response = new GenericResponse<bool>();

            var certificate = await _context.FindCoachCertificateAsync(id);
            if (certificate is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Certificate not found.";
                return response;
            }

            if (certificate.Status == CertificateStatus.Approved)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Certificate is already approved.";
                return response;
            }

            certificate.Status = CertificateStatus.Approved;
            certificate.ReviewedByAdminId = adminId;
            certificate.ReviewedAt = DateTime.UtcNow;
            certificate.RejectionReason = null;
            certificate.UpdatedAt = DateTime.UtcNow;

            _context.UpdateCoachCertificate(certificate);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Certificate approved successfully.";
            response.Data = true;
            return response;
        }

        public async Task<GenericResponse<bool>> RejectAsync(Guid id, string adminId, RejectCertificateRequest request)
        {
            var response = new GenericResponse<bool>();

            var validation = await _rejectValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var certificate = await _context.FindCoachCertificateAsync(id);
            if (certificate is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Certificate not found.";
                return response;
            }

            if (certificate.Status == CertificateStatus.Rejected)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Certificate is already rejected.";
                return response;
            }

            certificate.Status = CertificateStatus.Rejected;
            certificate.ReviewedByAdminId = adminId;
            certificate.ReviewedAt = DateTime.UtcNow;
            certificate.RejectionReason = request.Reason;
            certificate.UpdatedAt = DateTime.UtcNow;

            _context.UpdateCoachCertificate(certificate);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Certificate rejected.";
            response.Data = true;
            return response;
        }

        private static CertificateResponse MapToResponse(CoachCertificate c) => new()
        {
            Id = c.Id,
            CoachProfileId = c.CoachProfileId,
            Title = c.Title,
            IssuingOrganization = c.IssuingOrganization,
            IssuedDate = c.IssuedDate,
            FileUrl = c.FileUrl,
            FileName = c.FileName,
            FileType = c.FileType,
            Status = c.Status.ToString(),
            RejectionReason = c.RejectionReason,
            ReviewedAt = c.ReviewedAt,
            CreatedAt = c.CreatedAt
        };

        private static AdminCertificateResponse MapToAdminResponse(CoachCertificate c) => new()
        {
            Id = c.Id,
            CoachProfileId = c.CoachProfileId,
            CoachUserId = c.CoachProfile.UserId,
            Title = c.Title,
            IssuingOrganization = c.IssuingOrganization,
            IssuedDate = c.IssuedDate,
            FileUrl = c.FileUrl,
            FileName = c.FileName,
            FileType = c.FileType,
            Status = c.Status.ToString(),
            RejectionReason = c.RejectionReason,
            ReviewedAt = c.ReviewedAt,
            CreatedAt = c.CreatedAt
        };
    }
}
