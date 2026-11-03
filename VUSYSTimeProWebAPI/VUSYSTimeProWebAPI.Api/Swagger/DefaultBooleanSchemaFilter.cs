using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VUSYSTimeProWebAPI.Api.Swagger
{
    /// <summary>
    /// Sets default boolean values for Swagger/OpenAPI schemas.
    /// </summary>
    public sealed class DefaultBooleanSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Applies default boolean values to Swagger schema properties.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            try
            {
                if (schema is not OpenApiSchema openApiSchema ||
                    openApiSchema.Properties == null)
                {
                    return;
                }

                if (openApiSchema.Properties.TryGetValue("isDeleted", out IOpenApiSchema? isDeletedSchema) && isDeletedSchema is OpenApiSchema openApiIsDeletedSchema)
                {
                    openApiIsDeletedSchema.Default = false;
                    openApiIsDeletedSchema.Example = false;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}