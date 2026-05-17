namespace CoachingFit.Identity.Shared.DTOs.Requests
{
    public class RevokeTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}
