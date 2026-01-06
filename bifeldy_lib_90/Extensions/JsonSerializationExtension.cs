using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Resolvers;
using bifeldy_lib_90.TableView;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class JsonSerializationExtension {

        // TODO: Add additional JsonSerializerContext generated resolvers here
        private static readonly IJsonTypeInfoResolver[] JsonTypeInfoResolvers = [
            EnvVarJsonSerializerContext.Default,
            JsonSerDeJsonSerializerContext.Default,
            KunciRequestJsonSerializerContext.Default,
            ListApiDcJsonSerializerContext.Default,
            LoginInfoJsonSerializerContext.Default,
            RequestJsonSerializerContext.Default,
            ResponseJsonSerializerContext.Default,
            ServerConfigJsonSerializerContext.Default,
            UserSessionJsonSerializerContext.Default,
            API_KEY_T_JsonSerializerContext.Default,
            API_TOKEN_T_JsonSerializerContext.Default,
            DC_TABEL_DC_T_JsonSerializerContext.Default,
            DC_TABEL_IP_T_JsonSerializerContext.Default,
            DC_TABEL_V_JsonSerializerContext.Default,
            DC_USER_T_JsonSerializerContext.Default,
            KAFKA_SERVER_T_JsonSerializerContext.Default,
        ];

        public static IServiceCollection ConfigureHttpJsonOptionsEx(this IServiceCollection services, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended) {
            return services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.PropertyNamingPolicy = null;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;

                IJsonTypeInfoResolver[] resolvers = [
                    options.SerializerOptions.TypeInfoResolver!,
                    .. JsonTypeInfoResolvers,
                    .. jsonTypeInfoResolversExtended
                ];

                options.SerializerOptions.TypeInfoResolverChain.Clear();
                options.SerializerOptions.TypeInfoResolverChain.Add(new AutoRegisterTypeInfoResolver());

                IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(resolvers);
                options.SerializerOptions.TypeInfoResolverChain.Add(new JsonSerDeTypeInfoResolver(combined));
            });
        }

    }

}
