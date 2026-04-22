using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ITraineeProfileService
    {
        Task<GenericResponse<TraineeProfileResponse>> CreateAsync(CreateTraineeProfileRequest request, string userId);
        Task<GenericResponse<TraineeProfileResponse>> GetByIdAsync(Guid id);
        Task<GenericResponse<TraineeProfileResponse>> GetByUserIdAsync(string userId);
        Task<GenericResponse<TraineeProfileResponse>> UpdateAsync(UpdateTraineeProfileRequest request, string userId);
    }
}
