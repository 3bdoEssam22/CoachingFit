using CoachingFit.Identity.Core.Contracts;
using CoachingFit.Identity.Core.Entities;
using CoachingFit.Identity.Infrastructure.Data.Context;
using CoachingFit.Identity.Infrastructure.Data.DataSeed;
using CoachingFit.Identity.Infrastructure.ExternalServices;
using CoachingFit.Identity.Infrastructure.Services;
using CoachingFit.Identity.Services.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoachingFit.Identity.Infrastructure.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Identity
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                //Password settings
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                //MaxFailedAccessAttempts
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

            // Services
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddHostedService<RefreshTokenCleanupWorker>();
            services.AddScoped<IDataInitializer, DataInitializer>();

            // Email
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
