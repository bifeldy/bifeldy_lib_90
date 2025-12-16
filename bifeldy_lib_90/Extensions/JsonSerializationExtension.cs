using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.TableView;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class JsonSerializationExtension {

        // TODO: Add additional JsonSerializerContext generated resolvers here
        public static IJsonTypeInfoResolver[] JsonTypeInfoResolvers = [
            JsonSerDeJsonSerializerContext.Default,
            EnvVarJsonSerializerContext.Default,
            KunciRequestJsonSerializerContext.Default,
            ResponseJsonSerializerContext.Default,
            RequestJsonSerializerContext.Default,
            ServerConfigJsonSerializerContext.Default,
            DC_TABEL_DC_T_JsonSerializerContext.Default,
            DC_USER_T_JsonSerializerContext.Default,
        ];

        public static IServiceCollection ConfigureHttpJsonOptionsEx(this IServiceCollection services, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended) {
            return services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.PropertyNamingPolicy = null;

                foreach (IJsonTypeInfoResolver resolver in JsonTypeInfoResolvers.Concat(jsonTypeInfoResolversExtended)) {
                    options.SerializerOptions.TypeInfoResolverChain.Add(resolver);
                }
            });
        }

    }

}
