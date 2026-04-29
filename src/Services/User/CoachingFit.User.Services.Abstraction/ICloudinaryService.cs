using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Services.Abstraction
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}