using CoachingFit.Identity.Core.Entities;

namespace CoachingFit.Identity.Services.Abstraction
{
    public interface IRefreshTokenService
    {
        Task<(string PlaintextToken, RefreshToken Entity)> IssueAsync(
            string userId, CancellationToken ct = default);

        Task<RefreshToken?> FindByPlaintextAsync(
            string plaintext, CancellationToken ct = default);

        Task<(string PlaintextToken, RefreshToken Entity)> RotateAsync(
            RefreshToken current, CancellationToken ct = default);

        Task RevokeAsync(
            RefreshToken token, string reason, CancellationToken ct = default);

        Task RevokeAllForUserAsync(
            string userId, string reason, CancellationToken ct = default);
    }
}
