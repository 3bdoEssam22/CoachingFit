using CoachingFit.User.Core.Entities;
using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.User.Services
{
    public class CoachProfileService(
            IUserDbContext _context,
            IValidator<CreateCoachProfileRequest> _createValidator,
            IValidator<UpdateCoachProfileRequest> _updateValidator) : ICoachProfileService
    {
        public async Task<GenericResponse<CoachProfileResponse>> CreateAsync(
            CreateCoachProfileRequest request, string userId)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            // Check if profile already exists
            var exists = await _context.CoachProfiles
                .AnyAsync(c => c.UserId == userId);
            if (exists)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Coach profile already exists.";
                return response;
            }

            var profile = new CoachProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Gender = request.Gender,
                Bio = request.Bio,
                ExperienceYears = request.ExperienceYears,
                CreatedAt = DateTime.UtcNow
            };

            await _context.CoachProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = "Coach profile created successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<CoachProfileResponse>> GetByIdAsync(Guid id)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var profile = await _context.CoachProfiles.FindAsync(id);
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

        public async Task<GenericResponse<CoachProfileResponse>> GetByUserIdAsync(string userId)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var profile = await _context.CoachProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);
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
            UpdateCoachProfileRequest request, string userId)
        {
            var response = new GenericResponse<CoachProfileResponse>();

            var validation = await _updateValidator.ValidateAsync(request);
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
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Coach profile not found.";
                return response;
            }

            profile.Bio = request.Bio;
            profile.ExperienceYears = request.ExperienceYears;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.CoachProfiles.Update(profile);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Coach profile updated successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllPendingAsync(
            IEnumerable<string> pendingCoachUserIds)
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
                .ToListAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Pending coach profiles retrieved successfully.";
            response.Data = profiles.Select(MapToResponse);
            return response;
        }

        private static CoachProfileResponse MapToResponse(CoachProfile profile) => new()
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Gender = profile.Gender,
            Bio = profile.Bio,
            ExperienceYears = profile.ExperienceYears,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            CreatedAt = profile.CreatedAt
        };
    }
}
