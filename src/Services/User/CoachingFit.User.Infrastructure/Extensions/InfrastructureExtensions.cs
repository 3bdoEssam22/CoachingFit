using CoachingFit.User.Core.Contracts;
using CoachingFit.User.Infrastructure.Data.Context;
using CoachingFit.User.Infrastructure.ExternalServices;
using CoachingFit.User.Services;
using CoachingFit.User.Services.Abstraction;
using CoachingFit.User.Services.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoachingFit.User.Infrastructure.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // IUserDbContext → UserDbContext
            services.AddScoped<IUserDbContext>(sp =>
                sp.GetRequiredService<UserDbContext>());

            // Cloudinary
            services.Configure<CloudinarySettings>(
                configuration.GetSection("Cloudinary"));
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            // Services
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICoachProfileService, CoachProfileService>();
            services.AddScoped<ITraineeProfileService, TraineeProfileService>();
            services.AddScoped<ICoachCertificateService, CoachCertificateService>();

            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<CreateCoachProfileValidator>();

            return services;
        }
    }
}
