
using CoachingFit.Identity.API.Extensions;
using CoachingFit.Identity.API.Infrastructure.Idempotency;
using CoachingFit.Identity.Infrastructure.Extensions;
using CoachingFit.Identity.Services;
using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Services.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;

namespace CoachingFit.Identity.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Add services to the container.

            builder.Services.AddIdempotency();

            builder.Services.AddControllers(o => o.Filters.AddService<IdempotencyKeyFilter>());

            // Forwarded headers — honours X-Forwarded-For from YARP gateway so rate limiting sees real client IP
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("WebClients", policy =>
                {
                    if (builder.Environment.IsDevelopment())
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    else
                        policy.WithOrigins(
                                builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>() ?? [])
                            .AllowAnyHeader()
                            .WithMethods("GET", "POST", "PUT", "DELETE");
                });
            });

            // Rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("auth-limit", policy =>
                {
                    policy.PermitLimit = 10;
                    policy.Window = TimeSpan.FromMinutes(1);
                    policy.QueueLimit = 0;
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            #region Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token."
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
            #endregion

            // Infrastructure (DbContext + Identity + JwtService + DataInitializer)
            builder.Services.AddInfrastructure(builder.Configuration);

            // Services
            builder.Services.AddScoped<IAuthService, AuthService>();

            // FluentValidation
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterCoachValidator>();

            // JWT Authentication — fail fast if key is absent or too short
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
                throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits).");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            #endregion

            var app = builder.Build();

            await app.MigrateDatabaseAsync();
            await app.SeedDataAsync();

            // Configure the HTTP request pipeline.
            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
                app.UseHsts();

            app.UseHttpsRedirection();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("WebClients");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
