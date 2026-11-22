using Microsoft.OpenApi;

namespace VUSYSAICodeReviewWebAPI.Api.Extensions;

internal static class SwaggerExtensions
{
    public static void AddPortalSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "VUSYSAICodeReviewWebAPI.Api.xml"));
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "VUSYS AI Code Review Web API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
        });
    }
}
