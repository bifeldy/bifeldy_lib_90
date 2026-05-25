using bifeldy_lib_90.Libraries;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public static class AotResolverCache<T> {

        private static JsonTypeInfo<T> _cachedInfo;
        private static readonly Lock _lock = new();

        public static JsonTypeInfo<T> GetResolvedInfo(JsonTypeInfo<T> originalTypeInfo) {
            if (_cachedInfo != null) {
                return _cachedInfo;
            }

            lock (_lock) {
                if (_cachedInfo == null) {
                    var options = new JsonSerializerOptions() {
                        PropertyNamingPolicy = null,
                        PropertyNameCaseInsensitive = true,
                        TypeInfoResolver = new JsonSerDeTypeInfoResolver(originalTypeInfo.Options.TypeInfoResolver)
                    };

                    if (!options.Converters.Any(c => c is DecimalConverter)) {
                        options.Converters.Add(new DecimalConverter());
                    }

                    if (!options.Converters.Any(c => c is NullableDecimalConverter)) {
                        options.Converters.Add(new NullableDecimalConverter());
                    }

                    _cachedInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
                }
            }

            return _cachedInfo;
        }

    }

}
