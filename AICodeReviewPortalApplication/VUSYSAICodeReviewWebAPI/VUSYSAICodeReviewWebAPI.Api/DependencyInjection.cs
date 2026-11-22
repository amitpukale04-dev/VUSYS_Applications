using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VUSYSAICodeReviewWebAPI.Api.Extensions;
using VUSYSAICodeReviewWebAPI.Api.Filters;
using VUSYSAICodeReviewWebAPI.Api.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;
using VUSYSAICodeReviewWebAPI.Application.Features;

namespace VUSYSAICodeReviewWebAPI.Api;

internal static class DependencyInjection
{
    public static void AddPortalApi(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection("Auth").Get<AuthOptions>() ?? new();
        if (Encoding.UTF8.GetByteCount(auth.JwtKey) < 32 || auth.AdminPassword.Length < 5)
            throw new InvalidOperationException("Configure Auth:JwtKey (32+ bytes) and Auth:AdminPassword (12+ characters).");
        services.AddSingleton(auth);
        services.AddScoped<AuthenticationService>();
        services.AddScoped<JwtAuthorizationFilter>();
        services.AddScoped<ReviewService>();
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new
            {
                error = "Request validation failed.",
                errors = context.ModelState.Where(item => item.Value?.Errors.Count > 0)
                    .ToDictionary(item => item.Key, _ => new[] { "Invalid value." })
            });
        });

        services.AddCors(options => options.AddPolicy("frontend", policy => policy
            .WithOrigins(configuration["Cors:Origin"] ?? "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod()));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = AuthOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = AuthOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = AuthenticationService.SigningKey(auth),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
            options.Events = new()
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.Headers.WWWAuthenticate = "Bearer";
                    await context.Response.WriteAsJsonAsync(new { error = "A valid session is required." });
                }
            };
        });
        services.AddAuthorization();
        services.AddPortalSwagger();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan delay))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds)).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Request rate limit reached. Try again later." }, ct);
            };
            options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.Name ?? "unknown", _ => Limit(5, TimeSpan.FromMinutes(10))));
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => Limit(10, TimeSpan.FromMinutes(1))));
        });
    }

    private static FixedWindowRateLimiterOptions Limit(int permits, TimeSpan window)
        => new() { PermitLimit = permits, Window = window, QueueLimit = 0, AutoReplenishment = true };
}
