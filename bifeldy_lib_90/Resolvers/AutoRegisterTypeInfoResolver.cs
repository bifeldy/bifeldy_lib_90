using bifeldy_lib_90.Libraries;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Resolvers {

    public sealed class AutoRegisterTypeInfoResolver : IJsonTypeInfoResolver {

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            TypeRegistry.Register(type);
            return null;
        }
    }

}