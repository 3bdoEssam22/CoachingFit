using Microsoft.Extensions.Caching.Hybrid;

namespace CoachingFit.Identity.API.Infrastructure.Idempotency
{
    public static class IdempotencyExtensions
    {
        public static IServiceCollection AddIdempotency(this IServiceCollection services)
        {
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromHours(24)
                };
            });

            services.AddScoped<IdempotencyKeyFilter>();
            return services;
        }
    }
}
