namespace CoachingFit.Identity.Shared.DTOs.Responses
{
    public class AuthResponse
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
