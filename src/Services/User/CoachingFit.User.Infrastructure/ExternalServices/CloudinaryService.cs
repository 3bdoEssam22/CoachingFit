using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CoachingFit.User.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CoachingFit.User.Infrastructure.ExternalServices
{
    public class CloudinaryService(IOptions<CloudinarySettings> _options) : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary = new(new Account(
            _options.Value.CloudName,
            _options.Value.ApiKey,
            _options.Value.ApiSecret));

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "coachingfit/profiles",
                Transformation = new Transformation()
                    .Width(400).Height(400)
                    .Crop("fill")
                    .Gravity("face")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error is not null)
                throw new InvalidOperationException(
                    $"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }
    }
}
