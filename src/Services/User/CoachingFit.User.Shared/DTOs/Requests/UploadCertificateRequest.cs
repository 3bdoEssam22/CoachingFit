using Microsoft.AspNetCore.Http;

namespace CoachingFit.User.Shared.DTOs.Requests
{
    public class UploadCertificateRequest
    {
        public string Title { get; set; } = null!;
        public string IssuingOrganization { get; set; } = null!;
        public DateTime IssuedDate { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
