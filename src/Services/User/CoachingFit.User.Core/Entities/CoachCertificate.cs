using CoachingFit.User.Core.Enums;

namespace CoachingFit.User.Core.Entities
{
    public class CoachCertificate : BaseEntity
    {
        public Guid CoachProfileId { get; set; }
        public CoachProfile CoachProfile { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string IssuingOrganization { get; set; } = null!;
        public DateTime IssuedDate { get; set; }

        public string FileUrl { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string FileType { get; set; } = null!;

        public CertificateStatus Status { get; set; } = CertificateStatus.Pending;
        public string? RejectionReason { get; set; }
        public string? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
