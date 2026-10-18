using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TodoApi.Validation;

/// <summary>Documents the <c>issues</c> member that <see cref="ValidationProblems"/> adds to validation problems.</summary>
public class ValidationIssuesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(HttpValidationProblemDetails))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties[ValidationProblems.IssuesMember] = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Description = "The errors, each as an RFC 9457 error object with a code for clients to word it in their own language.",
            Items = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "detail", "code" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["pointer"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = "A JSON Pointer to the request field the error is about; absent when it is not about one.",
                    },
                    ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "What is wrong, in English." },
                    ["code"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = "Names the error: the code its rule declares, its validation attribute's name, or Identity's code.",
                    },
                },
            },
        };
        return Task.CompletedTask;
    }
}
