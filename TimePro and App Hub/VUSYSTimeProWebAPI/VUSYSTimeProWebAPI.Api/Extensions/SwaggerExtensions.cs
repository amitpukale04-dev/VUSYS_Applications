using System.Reflection;
using Microsoft.OpenApi;
using VUSYSTimeProWebAPI.Api.Swagger;

namespace VUSYSTimeProWebAPI.Api.Extensions;

public static class SwaggerExtensions
{
    private const string SwaggerDocumentName = "v1";
    private const string SwaggerTitle = "VUSYSTimePro API Services";

    /// <summary>
    /// Registers Swagger generation, XML comments and
    /// JWT Bearer security configuration.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SwaggerDocumentName,
                new OpenApiInfo
                {
                    Title = SwaggerTitle,
                    Version = SwaggerDocumentName,
                    Description = "An API to perform operations",

                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact =
                        new OpenApiContact
                        {
                            Name = "Amit Pukale",
                            Email = "amit.p@saanvitechin.com"
                        }

                        // License = new OpenApiLicense
                        // {
                        //     Name = "VUSYSTimePro API LICX",
                        //     Url = new Uri(
                        //         "https://example.com/license")
                        // }
                });

            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization Header Using the Bearer Scheme. Enter The Bearer [space] add your token in the text input. Example Bearer hfdjhfjdh87888",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

            options.AddSecurityRequirement(
                openApiDocument =>
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                openApiDocument),
                            new List<string>()
                        }
                    });
            options.SchemaFilter<DefaultBooleanSchemaFilter>();
        });

        return services;
    }

    /// <summary>
    /// Enables Swagger JSON generation and Swagger UI.
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"./{SwaggerDocumentName}/swagger.json",
                $"{SwaggerTitle} {SwaggerDocumentName}");

            options.RoutePrefix = "swagger";
            options.DocumentTitle = SwaggerTitle;
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
        });

        return app;
    }
}