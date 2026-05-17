using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, CancellationToken ct = default);
        Task<(string Url, string FileType)> UploadCertificateAsync(IFormFile file, CancellationToken ct = default);
    }
}