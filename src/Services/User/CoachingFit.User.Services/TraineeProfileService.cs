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
    public class TraineeProfileService(
        IUserDbContext _context,
        ICloudinaryService _cloudinaryService,
        ILogger<TraineeProfileService> _logger,
        IValidator<CreateTraineeProfileRequest> _createValidator,
        IValidator<UpdateTraineeProfileRequest> _updateValidator) : ITraineeProfileService
    {
        public async Task<GenericResponse<TraineeProfileResponse>> CreateAsync(
            CreateTraineeProfileRequest request, string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var exists = await _context.TraineeProfiles.AnyAsync(t => t.UserId == userId, ct);
            if (exists)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Trainee profile already exists.";
                return response;
            }

            string? photoUrl = null;
            if (request.Photo is not null)
            {
                try
                {
                    photoUrl = await _cloudinaryService.UploadImageAsync(request.Photo, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload profile photo for trainee {UserId}", userId);
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "Failed to upload profile photo. Please try again later.";
                    return response;
                }
            }

            var gender = Enum.Parse<Gender>(request.Gender, true);

            var profile = new TraineeProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Gender = gender,
                DateOfBirth = request.DateOfBirth,
                WeightKg = request.WeightKg,
                HeightCm = request.HeightCm,
                FitnessLevel = request.FitnessLevel,
                Goals = request.Goals,
                MedicalNotes = request.MedicalNotes,
                ProfilePhotoUrl = photoUrl
            };

            await _context.AddTraineeProfileAsync(profile, ct);
            await _context.SaveChangesAsync(ct);

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = "Trainee profile created successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<TraineeProfileResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var profile = await _context.FindTraineeProfileAsync(id, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Trainee profile not found.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Trainee profile retrieved successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<TraineeProfileResponse>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var profile = await _context.TraineeProfiles
                .FirstOrDefaultAsync(t => t.UserId == userId, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Trainee profile not found.";
                return response;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Trainee profile retrieved successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<TraineeProfileResponse>> UpdateAsync(
            UpdateTraineeProfileRequest request, string userId, CancellationToken ct = default)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var validation = await _updateValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var profile = await _context.TraineeProfiles
                .FirstOrDefaultAsync(t => t.UserId == userId, ct);
            if (profile is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Trainee profile not found.";
                return response;
            }

            profile.WeightKg = request.WeightKg;
            profile.HeightCm = request.HeightCm;
            profile.FitnessLevel = request.FitnessLevel;
            profile.Goals = request.Goals;
            profile.MedicalNotes = request.MedicalNotes;
            profile.UpdatedAt = DateTime.UtcNow;

            if (request.Photo is not null)
            {
                try
                {
                    profile.ProfilePhotoUrl = await _cloudinaryService.UploadImageAsync(request.Photo, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload profile photo for trainee {UserId}", userId);
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "Failed to upload profile photo. Please try again later.";
                    return response;
                }
            }

            _context.UpdateTraineeProfile(profile);
            await _context.SaveChangesAsync(ct);

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Trainee profile updated successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        private static TraineeProfileResponse MapToResponse(TraineeProfile profile) => new()
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Gender = profile.Gender.ToString(),
            DateOfBirth = profile.DateOfBirth,
            WeightKg = profile.WeightKg,
            HeightCm = profile.HeightCm,
            FitnessLevel = profile.FitnessLevel,
            Goals = profile.Goals,
            MedicalNotes = profile.MedicalNotes,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            CreatedAt = profile.CreatedAt
        };
    }
}