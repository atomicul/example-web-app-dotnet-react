using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TodoApi;

/// <summary>Documents the <c>codes</c> member that <see cref="ValidationCodes"/> adds to validation problems.</summary>
public class ValidationCodesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(HttpValidationProblemDetails))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties[ValidationCodes.Member] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "The codes of the errors, keyed like errors: each names the error at the same position, "
                + "for clients to word in their own language, or is null when the error has none.",
            AdditionalProperties = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            },
        };
        return Task.CompletedTask;
    }
}
