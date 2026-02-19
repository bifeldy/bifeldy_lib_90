using bifeldy_lib_90.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Transformers {

    public sealed class IgnorePropertySchemaTransformer : IOpenApiSchemaTransformer {

        public Task TransformAsync(
            OpenApiSchema schema,
            OpenApiSchemaTransformerContext context,
            CancellationToken cancellationToken
        ) {
            if (schema.Properties is null) {
                return Task.CompletedTask;
            }

            JsonTypeInfo jsonTypeInfo = context.JsonTypeInfo;
            if (jsonTypeInfo is null) {
                return Task.CompletedTask;
            }

            if (jsonTypeInfo.CreateObject?.Invoke() is not JsonSerDe model) {
                return Task.CompletedTask;
            }

            string[] hidden = model.HiddenProperties();
            if (hidden.Length == 0) {
                return Task.CompletedTask;
            }

            foreach (string prop in hidden) {
                _ = schema.Properties.Remove(prop);
            }

            return Task.CompletedTask;
        }

    }

}