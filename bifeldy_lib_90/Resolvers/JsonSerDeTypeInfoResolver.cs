using bifeldy_lib_90.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class JsonSerDeTypeInfoResolver : IJsonTypeInfoResolver {

        private readonly IJsonTypeInfoResolver[] _innerResolvers;

        public JsonSerDeTypeInfoResolver(IJsonTypeInfoResolver[] innerResolvers) {
            this._innerResolvers = innerResolvers;
        }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            JsonTypeInfo typeInfo = null;

            foreach (IJsonTypeInfoResolver resolver in this._innerResolvers) {
                typeInfo = resolver.GetTypeInfo(type, options);
                if (typeInfo != null) {
                    break;
                }
            }

            if (typeInfo is null) {
                return null;
            }

            if (typeInfo.Kind != JsonTypeInfoKind.Object) {
                return typeInfo;
            }

            if (!typeof(JsonSerDe).IsAssignableFrom(typeInfo.Type)) {
                return typeInfo;
            }

            if (typeInfo.CreateObject?.Invoke() is not JsonSerDe model) {
                return typeInfo;
            }

            string[] hidden = model.HiddenProperties();
            if (hidden.Length == 0) {
                return typeInfo;
            }

            foreach (JsonPropertyInfo prop in typeInfo.Properties) {
                if (hidden.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) {
                    prop.ShouldSerialize = (_, _) => false;
                }
            }

            return typeInfo;
        }

    }

}
