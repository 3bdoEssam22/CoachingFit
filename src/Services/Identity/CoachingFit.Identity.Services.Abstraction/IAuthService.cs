using CoachingFit.Identity.Shared.DTOs.Requests;
using CoachingFit.Identity.Shared.DTOs.Responses;
using CoachingFit.Identity.Shared.Wrappers;

namespace CoachingFit.Identity.Services.Abstraction
{
    public interface IAuthService
    {
        Task<GenericResponse<AuthResponse>> RegisterCoachAsync(RegisterCoachRequest request);
        Task<GenericResponse<AuthResponse>> RegisterTraineeAsync(RegisterTraineeRequest request);
        Task<GenericResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<GenericResponse<AuthResponse>> GetCurrentUserAsync(string userId);
        Task<GenericResponse<bool>> ConfirmEmailAsync(string userId, string token);
        Task<GenericResponse<bool>> ResendConfirmationEmailAsync(string email, string baseUrl);
        Task<GenericResponse<bool>> ActivateCoachAsync(string coachId);
    }
}