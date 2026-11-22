using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Persistence;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;
using VUSYSAICodeReviewWebAPI.Infrastructure.Persistence;
using VUSYSAICodeReviewWebAPI.Infrastructure.Persistence.Connections;
using VUSYSAICodeReviewWebAPI.Infrastructure.Services;

namespace VUSYSAICodeReviewWebAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GitHubOptions>(configuration.GetSection("GitHub"));
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IGitHubService, GitHubService>(client => client.Timeout = TimeSpan.FromSeconds(40))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddHttpClient<IGeminiService, GeminiService>(client => client.Timeout = TimeSpan.FromSeconds(100))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<DbConnectionHelper>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        return services;
    }
}
