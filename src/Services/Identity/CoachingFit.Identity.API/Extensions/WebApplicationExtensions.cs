using CoachingFit.Identity.Core.Contracts;
using CoachingFit.Identity.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.Identity.API.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await db.Database.MigrateAsync();
            return app;
        }

        public static async Task<WebApplication> SeedDataAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>();
            await initializer.InitializeAsync();
            return app;
        }

    }
}
