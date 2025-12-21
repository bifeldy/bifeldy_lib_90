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

            if (!typeof(JsonSerDe).IsAssignableFrom(info.Type)) {
                return info;
            }

            if (info.Kind == JsonTypeInfoKind.Object &&
                info.CreateObject?.Invoke() is JsonSerDe model) {

                string[] hidden = model.HiddenProperties();

                foreach (JsonPropertyInfo prop in info.Properties) {
                    if (hidden.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)) {
                        prop.ShouldSerialize = (_, _) => false;
                    }
                }
            }

            return info;
        }

    }

}
