using bifeldy_lib_90.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class JsonSerDeTypeInfoResolver : IJsonTypeInfoResolver {

        private readonly IJsonTypeInfoResolver _inner;

        public JsonSerDeTypeInfoResolver(IJsonTypeInfoResolver inner) {
            this._inner = inner;
        }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            JsonTypeInfo info = this._inner.GetTypeInfo(type, options);

            if (info == null) {
                return null;
            }

            if (info.Kind == JsonTypeInfoKind.Object && typeof(JsonSerDe).IsAssignableFrom(info.Type)) {
                foreach (JsonPropertyInfo prop in info.Properties) {
                    Func<object, object, bool> originalShouldSerialize = prop.ShouldSerialize;

                    prop.ShouldSerialize = (obj, propValue) => {
                        if (obj is JsonSerDe entity) {
                            string[] hidden = entity.HiddenProperties();
                            if (hidden != null && hidden.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) {
                                return false;
                            }
                        }

                        return originalShouldSerialize == null || originalShouldSerialize(obj, propValue);
                    };
                }
            }

            return info;
        }

    }

}