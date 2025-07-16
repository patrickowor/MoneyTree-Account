namespace Moneytree.Account.Src.Internal.Filters;


using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

public class OpenApiSnakeCaseSchemaFilter : IOpenApiSchemaTransformer {
    static readonly SnakeCaseNamingStrategy namingStrategy = new();
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken token){
            var prop = schema.Properties;
            schema.Properties = ToSnakeCase(prop).Properties;
            return Task.CompletedTask;
    }

    static OpenApiSchema ToSnakeCase(IDictionary<string, OpenApiSchema> prop, bool parent = true){
            var newProp = new Dictionary<string, OpenApiSchema>{};
            foreach (var (key, value) in prop)
            {
                string normalizedinput = namingStrategy
                    .GetPropertyName(key, hasSpecifiedName: false);

                if (value.Type == "object"){
                    newProp[normalizedinput] = ToSnakeCase(value.Properties, false);
                } else {
                    newProp[normalizedinput] = value;
                }
            }

            return new OpenApiSchema{
                Type = "object",
                Properties = newProp
            };
    }
}