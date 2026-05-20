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
    public class CoachProfileService(
        IUserDbContext _context,
        ICloudinaryService _cloudinaryService,
        ILogger<CoachProfileService> _logger,
        TimeProvider _timeProvider,
        IValidator<CreateCoachProfileRequest> _createValidator,
        IValidator<UpdateCoachProfileRequest> _updateValidator) : ICoachProfileService
    {
        public async Task<GenericResponse<CoachProfileResponse>> CreateAsync(
            CreateCoachProfileRequest request, string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var exists = await _context.CoachProfiles.AnyAsync(c => c.UserId == userId, ct);
            if (exists)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Coach profile already exists.";
                return response;
            }

            string? photoUrl = null;
            if (request.Photo is not null)
            {
                try
                {
                    photoUrl = await _cloudinaryService.UploadImageAsync(request.Photo, ct);
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
                {
                    _logger.LogError(ex, "Failed to upload profile photo for coach {UserId}", userId);
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "Failed to upload profile photo. Please try again later.";
                    return response;
                }
            }

            var gender = Enum.Parse<Gender>(request.Gender, true);

            var profile = new CoachProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Gender = gender,
                Bio = request.Bio,
                ExperienceYears = request.ExperienceYears,
                ProfilePhotoUrl = photoUrl
            };

            await _context.AddCoachProfileAsync(profile, ct);
            await _context.SaveChangesAsync(ct);

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = "Coach profile created successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<CoachProfileResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var profile = await _context.FindCoachProfileAsync(id, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach profile retrieved successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<CoachProfileResponse>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach profile retrieved successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<CoachProfileResponse>> UpdateAsync(
            UpdateCoachProfileRequest request, string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            profile.Bio = request.Bio;
            profile.ExperienceYears = request.ExperienceYears;
            profile.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            if (request.Photo is not null)
            {
                try
                {
                    profile.ProfilePhotoUrl = await _cloudinaryService.UploadImageAsync(request.Photo, ct);
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
                {
                    _logger.LogError(ex, "Failed to upload profile photo for coach {UserId}", userId);
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "Failed to upload profile photo. Please try again later.";
                    return response;
                }
            }

            _context.UpdateCoachProfile(profile);
            await _context.SaveChangesAsync(ct);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach profile updated successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllPendingAsync(
            IEnumerable<string> pendingCoachUserIds, CancellationToken ct = default)
        {
            var response = new GenericResponse<IEnumerable<CoachProfileResponse>>();

            var pendingIds = (pendingCoachUserIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .ToHashSet();

            if (pendingIds.Count == 0)
            {
                response.StatusCode = StatusCodes.Status200OK;
                response.Message = "No pending coach profiles found.";
                response.Data = Enumerable.Empty<CoachProfileResponse>();
                return response;
            }

            var profiles = await _context.CoachProfiles
                .Where(c => pendingIds.Contains(c.UserId))
                .ToListAsync(ct);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Pending coach profiles retrieved successfully.";
            response.Data = profiles.Select(MapToResponse);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllAsync(CancellationToken ct = default)
        {
            var profiles = await _context.CoachProfiles.AsNoTracking().ToListAsync(ct);
            return new GenericResponse<IEnumerable<CoachProfileResponse>>
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "All coach profiles retrieved successfully.",
                Data = profiles.Select(MapToResponse)
            };
        }

        private static CoachProfileResponse MapToResponse(CoachProfile profile) => new()
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Gender = profile.Gender.ToString(),
            Bio = profile.Bio,
            ExperienceYears = profile.ExperienceYears,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            CreatedAt = profile.CreatedAt
        };
    }
}