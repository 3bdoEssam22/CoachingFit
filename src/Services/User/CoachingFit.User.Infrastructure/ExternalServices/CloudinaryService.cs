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

        public async Task<string> UploadImageAsync(IFormFile file, CancellationToken ct = default)
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

        public async Task<(string Url, string FileType)> UploadCertificateAsync(IFormFile file, CancellationToken ct = default)
        {
            await using var stream = file.OpenReadStream();

            if (file.ContentType == "application/pdf")
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "coachingfit/certificates"
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error is not null)
                    throw new InvalidOperationException(
                        $"Cloudinary upload failed: {result.Error.Message}");

                return (result.SecureUrl.ToString(), "pdf");
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "coachingfit/certificates"
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error is not null)
                    throw new InvalidOperationException(
                        $"Cloudinary upload failed: {result.Error.Message}");

                return (result.SecureUrl.ToString(), "image");
            }
        }
    }
}
