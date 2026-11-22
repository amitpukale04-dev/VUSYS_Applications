using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VUSYSTimeProWebAPI.Application.Models.Authentication;

namespace VUSYSTimeProWebAPI.Api.Extensions;

public static class JwtAuthenticationExtensions
{
    private const string JwtConfigurationSection =
        "JWTKeys";

    /// <summary>
    /// Registers JWT Bearer authentication and authorization.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtSettings jwtSettings = configuration.GetSection(JwtConfigurationSection).Get<JwtSettings>()
            ?? throw new InvalidOperationException($"The {JwtConfigurationSection} configuration " + "section is missing or invalid.");

        if (string.IsNullOrWhiteSpace(jwtSettings.IssuerSigningKey))
        {
            throw new InvalidOperationException($"{JwtConfigurationSection}:IssuerSigningKey " + "is missing.");
        }

        if (jwtSettings.ValidateIssuer && string.IsNullOrWhiteSpace(jwtSettings.ValidIssuer))
        {
            throw new InvalidOperationException($"{JwtConfigurationSection}:ValidIssuer " + "is required when ValidateIssuer is enabled.");
        }

        if (jwtSettings.ValidateAudience && (jwtSettings.ValidAudiences is null || jwtSettings.ValidAudiences.Count == 0))
        {
            throw new InvalidOperationException($"{JwtConfigurationSection}:ValidAudiences " + "is required when ValidateAudience is enabled.");
        }

        // Register the same settings instance so that it can
        // be injected into RequestLoggingMiddleware and services.
        services.AddSingleton(jwtSettings);

        byte[] signingKey = Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = jwtSettings.ValidateIssuer,
                        ValidateAudience = jwtSettings.ValidateAudience,
                        ValidateLifetime = jwtSettings.ValidateLifetime,
                        ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                        ValidIssuer = jwtSettings.ValidIssuer,
                        ValidAudiences = jwtSettings.ValidAudiences,
                        RequireExpirationTime = jwtSettings.RequireExpirationTime,
                        IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                options.Events =
                    new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("Authentication failed: " + context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Console.WriteLine("Authorization failed. " + "Sending 401 response.");
                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            Console.WriteLine("Authorization failed. " + "Sending 403 response.");
                            return Task.CompletedTask;
                        }
                    };
            });
        services.AddAuthorization();
        return services;
    }
}