using System.IO.Compression;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using VUSYSTimeProWebAPI.Api.Extensions;
using VUSYSTimeProWebAPI.Api.Filters;
using VUSYSTimeProWebAPI.Api.Services.Files;
using VUSYSTimeProWebAPI.Api.Services.FileUploads;
using VUSYSTimeProWebAPI.Application.Common.Options;

namespace VUSYSTimeProWebAPI.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IncludeFields = true;
                });

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 500L * 1024 * 1024;
            });

            // Enable response compression for large API responses,
            // including JSON responses containing Base64 report data.
            services.AddResponseCompression(options =>
            {
                // Enable compression for HTTPS responses.
                options.EnableForHttps = true;

                // Prefer Brotli when supported by the client.
                options.Providers.Add<BrotliCompressionProvider>();

                // Use GZIP as a fallback.
                options.Providers.Add<GzipCompressionProvider>();

                // Preserve all default compressible MIME types and
                // explicitly include JSON-related response types.
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[]
                    {
                        ContentTypeConstants.JSON_CONTENTTYPE,
                        ContentTypeConstants.PROBLEM_JSON_CONTENTTYPE,
                        ContentTypeConstants.TEXT_JSON_CONTENTTYPE,
                        ContentTypeConstants.BINARY_CONTENTTYPE
                    });
            });

            // Fastest provides a good balance between CPU usage
            // and response-size reduction for web API requests.
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.AddOptions<MyApplicationKeysOptions>()
                .Bind(configuration.GetSection(MyApplicationKeysOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<MyApplicationKeysOptions>>().Value);

            services.AddOptions<GeminiAIKeysOptions>()
                .Bind(configuration.GetSection(GeminiAIKeysOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<GeminiAIKeysOptions>>().Value);

            services.AddHttpContextAccessor();

            services.AddScoped<JwtAuthorizationFilter>();

            services.AddApiCors();

            services.AddScoped<IFormFileService, FormFileService>();

            services.AddScoped<IFileUploadService, FileUploadService>();

            return services;
        }
    }
}