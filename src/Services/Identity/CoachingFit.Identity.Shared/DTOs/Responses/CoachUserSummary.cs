namespace CoachingFit.Identity.Shared.DTOs.Responses;

public class CoachUserSummary
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }
}
