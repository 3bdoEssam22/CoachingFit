using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICoachProfileService
    {
        Task<GenericResponse<CoachProfileResponse>> CreateAsync(CreateCoachProfileRequest request, string userId);
        Task<GenericResponse<CoachProfileResponse>> GetByIdAsync(Guid id);
        Task<GenericResponse<CoachProfileResponse>> GetByUserIdAsync(string userId);
        Task<GenericResponse<CoachProfileResponse>> UpdateAsync(UpdateCoachProfileRequest request, string userId);
        Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllPendingAsync(IEnumerable<string> pendingCoachUserIds);
    }
}