using CoachingFit.User.API.Extensions;
using CoachingFit.User.API.Infrastructure.Idempotency;
using CoachingFit.User.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using System.Text;

namespace CoachingFit.User.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Services

            builder.Services.AddIdempotency();

            builder.Services.AddControllers(o => o.Filters.AddService<IdempotencyKeyFilter>())
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Forwarded headers — honours X-Forwarded-For from YARP gateway so metrics/logging see real client IP
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
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

            // Infrastructure (DbContext + services + validators)
            builder.Services.AddInfrastructure(builder.Configuration);

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

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
                app.UseHsts();


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("WebClients");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}