using CoachingFit.User.Shared.DTOs.Requests;
using CoachingFit.User.Shared.DTOs.Responses;
using CoachingFit.User.Shared.Wrappers;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICoachCertificateService
    {
        Task<GenericResponse<CertificateResponse>> UploadAsync(UploadCertificateRequest request, string userId, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<CertificateResponse>>> GetMyCertificatesAsync(string userId, CancellationToken ct = default);
        Task<GenericResponse<CertificateResponse>> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
        Task<GenericResponse<bool>> DeleteAsync(Guid id, string userId, CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetPendingAsync(CancellationToken ct = default);
        Task<GenericResponse<IEnumerable<AdminCertificateResponse>>> GetByCoachUserIdAsync(string coachUserId, CancellationToken ct = default);
        Task<GenericResponse<bool>> ApproveAsync(Guid id, string adminId, CancellationToken ct = default);
        Task<GenericResponse<bool>> RejectAsync(Guid id, string adminId, RejectCertificateRequest request, CancellationToken ct = default);
    }
}
