using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICoachCertificateService
    {
        Task<GenericResponse<CertificateResponse>> UploadAsync(UploadCertificateRequest request, string userId);
        Task<GenericResponse<IEnumerable<CertificateResponse>>> GetMyCertificatesAsync(string userId);
        Task<GenericResponse<CertificateResponse>> GetByIdAsync(Guid id, string userId);
        Task<GenericResponse<bool>> DeleteAsync(Guid id, string userId);
        Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetPendingAsync();
        Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetByCoachUserIdAsync(string coachUserId);
        Task<GenericResponse<bool>> ApproveAsync(Guid id, string adminId);
        Task<GenericResponse<bool>> RejectAsync(Guid id, string adminId, RejectCertificateRequest request);
    }
}
