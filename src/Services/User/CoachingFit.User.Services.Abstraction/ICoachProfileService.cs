using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICoachProfileService
    {
        Task<GenericResponse<CoachProfileResponse>> CreateAsync(CreateCoachProfileRequest request, string userId, CancellationToken ct = default);
        Task<GenericResponse<CoachProfileResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<GenericResponse<CoachProfileResponse>> GetByUserIdAsync(string userId, CancellationToken ct = default);
        Task<GenericResponse<CoachProfileResponse>> UpdateAsync(UpdateCoachProfileRequest request, string userId, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllPendingAsync(IEnumerable<string> pendingCoachUserIds, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<CoachProfileResponse>>> GetAllAsync(CancellationToken ct = default);
    }
}