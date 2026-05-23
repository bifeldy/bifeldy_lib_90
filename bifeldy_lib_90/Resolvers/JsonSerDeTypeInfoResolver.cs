using bifeldy_lib_90.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class JsonSerDeTypeInfoResolver : IJsonTypeInfoResolver {

        private readonly IJsonTypeInfoResolver _inner;

        private static readonly ConditionalWeakTable<JsonPropertyInfo, object> _modifiedProperties = [];

        public JsonSerDeTypeInfoResolver(IJsonTypeInfoResolver inner) {
            this._inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            JsonTypeInfo info = this._inner.GetTypeInfo(type, options);
            if (info == null || info.Kind != JsonTypeInfoKind.Object) {
                return info;
            }

            if (typeof(JsonSerDe).IsAssignableFrom(info.Type)) {
                foreach (JsonPropertyInfo prop in info.Properties) {
                    if (_modifiedProperties.TryGetValue(prop, out _)) {
                        continue;
                    }

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

                    _ = _modifiedProperties.TryAdd(prop, null);
                }
            }

            return info;
        }

    }

}