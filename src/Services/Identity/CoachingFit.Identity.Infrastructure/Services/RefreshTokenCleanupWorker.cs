using CoachingFit.Identity.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachingFit.Identity.Infrastructure.Services
{
    public class RefreshTokenCleanupWorker(
        IServiceScopeFactory _scopeFactory,
        TimeProvider _timeProvider,
        ILogger<RefreshTokenCleanupWorker> _logger) : BackgroundService
    {
        private static readonly TimeSpan RunAtUtc = TimeSpan.FromHours(3);
        private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(2);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextRun();
                _logger.LogInformation(
                    "RefreshTokenCleanupWorker scheduled for {NextRunUtc:o} (in {Delay})",
                    _timeProvider.GetUtcNow().UtcDateTime.Add(delay), delay);

                try
                {
                    await Task.Delay(delay, _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                await RunCleanupAsync(stoppingToken);
            }
        }

        private TimeSpan TimeUntilNextRun()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var today = now.Date.Add(RunAtUtc);
            var next = now < today ? today : today.AddDays(1);
            return next - now;
        }

        private async Task RunCleanupAsync(CancellationToken ct)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var cutoff = _timeProvider.GetUtcNow().UtcDateTime - GracePeriod;

                var deleted = await db.RefreshTokens
                    .Where(t => (t.RevokedAt != null && t.RevokedAt < cutoff)
                             || (t.RevokedAt == null && t.ExpiresAt < cutoff))
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation(
                    "RefreshTokenCleanupWorker deleted {Count} rows (cutoff {Cutoff:o})",
                    deleted, cutoff);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "RefreshTokenCleanupWorker run failed");
            }
        }
    }
}
