namespace CoachingFit.User.Shared.DTOs.Responses
{
    public class CertificateResponse
    {
        public Guid Id { get; set; }
        public Guid CoachProfileId { get; set; }
        public string Title { get; set; } = null!;
        public string IssuingOrganization { get; set; } = null!;
        public DateTime IssuedDate { get; set; }
        public string FileUrl { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
