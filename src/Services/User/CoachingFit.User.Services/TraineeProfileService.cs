using CoachingFit.User.Core.Entities;
using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.User.Services
{
    public class TraineeProfileService(
            IUserDbContext _context,
            IValidator<CreateTraineeProfileRequest> _createValidator,
            IValidator<UpdateTraineeProfileRequest> _updateValidator) : ITraineeProfileService
    {
        public async Task<GenericResponse<TraineeProfileResponse>> CreateAsync(
            CreateTraineeProfileRequest request, string userId)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var exists = await _context.TraineeProfiles
                .AnyAsync(t => t.UserId == userId);
            if (exists)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Trainee profile already exists.";
                return response;
            }

            var profile = new TraineeProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                WeightKg = request.WeightKg,
                HeightCm = request.HeightCm,
                FitnessLevel = request.FitnessLevel,
                Goals = request.Goals,
                MedicalNotes = request.MedicalNotes,
                CreatedAt = DateTime.UtcNow
            };

            await _context.TraineeProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status201Created;
            response.Message = "Trainee profile created successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        public async Task<GenericResponse<TraineeProfileResponse>> GetByIdAsync(Guid id)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var profile = await _context.TraineeProfiles.FindAsync(id);
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

        public async Task<GenericResponse<TraineeProfileResponse>> GetByUserIdAsync(string userId)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var profile = await _context.TraineeProfiles
                .FirstOrDefaultAsync(t => t.UserId == userId);
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
            UpdateTraineeProfileRequest request, string userId)
        {
            var response = new GenericResponse<TraineeProfileResponse>();

            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
                return response;
            }

            var profile = await _context.TraineeProfiles
                .FirstOrDefaultAsync(t => t.UserId == userId);
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

            _context.TraineeProfiles.Update(profile);
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Trainee profile updated successfully.";
            response.Data = MapToResponse(profile);
            return response;
        }

        private static TraineeProfileResponse MapToResponse(TraineeProfile profile) => new()
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Gender = profile.Gender,
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
