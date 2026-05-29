using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Wrappers;

namespace CoachingFit.Identity.Services.Abstraction
{
    public interface IAuthService
    {
        Task<GenericResponse<AuthResponse>> RegisterCoachAsync(RegisterCoachRequest request, string baseUrl, CancellationToken ct = default);
        Task<GenericResponse<AuthResponse>> RegisterTraineeAsync(RegisterTraineeRequest request, string baseUrl, CancellationToken ct = default);
        Task<GenericResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<GenericResponse<AuthResponse>> GetCurrentUserAsync(string userId, CancellationToken ct = default);
        Task<GenericResponse<bool>> ConfirmEmailAsync(string userId, string token, CancellationToken ct = default);
        Task<GenericResponse<bool>> ResendConfirmationEmailAsync(string email, string baseUrl, CancellationToken ct = default);
        Task<GenericResponse<bool>> ActivateCoachAsync(string coachId, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<string>>> GetPendingCoachUserIdsAsync(CancellationToken ct = default);
        Task<GenericResponse<AuthResponse>> RefreshTokenAsync(string plaintextRefreshToken, CancellationToken ct = default);
        Task<GenericResponse<bool>> RevokeTokenAsync(string plaintextRefreshToken, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<string>>> GetAllCoachUserIdsAsync(CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<string>>> GetAllTraineeUserIdsAsync(CancellationToken ct = default);
        Task<GenericResponse<AdminStatsResponse>> GetStatsAsync(CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<CoachUserSummary>>> GetCoachDetailsAsync(CancellationToken ct = default);
        Task<GenericResponse<CoachUserSummary>> GetCoachSummaryAsync(string coachId, CancellationToken ct = default);
        Task<GenericResponse<bool>> RejectCoachAsync(string coachId, string reason, CancellationToken ct = default);
        Task<GenericResponse<bool>> DeactivateCoachAsync(string coachId, string reason, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<TraineeUserSummary>>> GetTraineeDetailsAsync(CancellationToken ct = default);
    }
}