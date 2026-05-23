using bifeldy_lib_90.Libraries;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class AutoRegisterTypeInfoResolver : IJsonTypeInfoResolver {

        private static readonly ConcurrentDictionary<Type, bool> _processedTypes = new();

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            if (type == null) {
                return null;
            }

            if (_processedTypes.TryAdd(type, true)) {
                TypeRegistry.Register(type);
            }

            return null;
        }

    }

}