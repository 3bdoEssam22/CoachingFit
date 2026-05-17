using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ITraineeProfileService
    {
        Task<GenericResponse<TraineeProfileResponse>> CreateAsync(CreateTraineeProfileRequest request, string userId, CancellationToken ct = default);
        Task<GenericResponse<TraineeProfileResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<GenericResponse<TraineeProfileResponse>> GetByUserIdAsync(string userId, CancellationToken ct = default);
        Task<GenericResponse<TraineeProfileResponse>> UpdateAsync(UpdateTraineeProfileRequest request, string userId, CancellationToken ct = default);
    }
}
