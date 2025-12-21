using bifeldy_lib_90.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class JsonSerDeTypeInfoResolver : IJsonTypeInfoResolver {

        private readonly IJsonTypeInfoResolver[] _innerResolvers;

        [UnconditionalSuppressMessage(
            "Trimming", "IL2026",
            Justification = "Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"
        )]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050",
            Justification = "Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling."
        )]
        private readonly IJsonTypeInfoResolver _fallbackResolver = new DefaultJsonTypeInfoResolver();

        public JsonSerDeTypeInfoResolver(IJsonTypeInfoResolver[] innerResolvers) {
            this._innerResolvers = innerResolvers;
        }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            if (
                options.TypeInfoResolver?.GetType().Name
                    .Contains("OpenApiJsonSchemaContext", StringComparison.InvariantCultureIgnoreCase) == true
            ) {
                return null;
            }

            if (
                !typeof(JsonSerDe).IsAssignableFrom(type) ||
                typeof(IResult).IsAssignableFrom(type) ||
                typeof(Task).IsAssignableFrom(type) ||
                typeof(ValueTask).IsAssignableFrom(type)
            ) {
                return this._fallbackResolver.GetTypeInfo(type, options);
            }

            foreach (IJsonTypeInfoResolver resolver in _innerResolvers) {
                JsonTypeInfo info = resolver.GetTypeInfo(type, options);

                if (info != null) {
                    if (info.Kind == JsonTypeInfoKind.Object && typeof(JsonSerDe).IsAssignableFrom(info.Type)) {
                        if (info.CreateObject?.Invoke() is JsonSerDe model) {
                            string[] hidden = model.HiddenProperties();

                            if (hidden.Length > 0) {
                                foreach (JsonPropertyInfo prop in info.Properties) {
                                    if (hidden.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) {
                                        prop.ShouldSerialize = (_, _) => false;
                                    }
                                }
                            }
                        }
                    }

                    return info;
                }
            }

            return this._fallbackResolver.GetTypeInfo(type, options);
        }

    }

}
