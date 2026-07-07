using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Resolvers;
using bifeldy_lib_90.TableView;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class JsonSerializationExtension {

        // TODO: Add additional JsonSerializerContext generated resolvers here
        private static readonly IJsonTypeInfoResolver[] JsonTypeInfoResolvers = [
            CDynamicClassPropertyJsonSerializerContext.Default,
            CDynamicClassPropertyV2JsonSerializerContext.Default,
            CPocoModelJsonSerializerContext.Default,
            CTableClassModelJsonSerializerContext.Default,
            EnvVarJsonSerializerContext.Default,
            InfoDcJsonSerializerContext.Default,
            InputJsonDcPingPongJsonSerializerContext.Default,
            JobTrackerJsonContext.Default,
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
            DC_LISTMAILSERVER_T_JsonSerializerContext.Default,
            DC_TABEL_DC_T_JsonSerializerContext.Default,
            DC_TABEL_IP_T_JsonSerializerContext.Default,
            DC_TABEL_V_JsonSerializerContext.Default,
            DC_USER_T_JsonSerializerContext.Default,
            KAFKA_SERVER_T_JsonSerializerContext.Default,
        ];

        private static List<IJsonTypeInfoResolver> ActiveJsonTypeInfoResolvers = null;
        private static readonly Lock _resolverLock = new();

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safety guaranteed by JsonTypeInfo usage.")]
        public static JsonSerializerOptions ConfigureJson(JsonSerializerOptions options = null, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended = null) {
            options ??= new JsonSerializerOptions();

            options.PropertyNamingPolicy = null;
            options.PropertyNameCaseInsensitive = true;

            if (!options.Converters.Any(c => c is DecimalConverter)) {
                options.Converters.Add(new DecimalConverter());
            }

            if (!options.Converters.Any(c => c is NullableDecimalConverter)) {
                options.Converters.Add(new NullableDecimalConverter());
            }

            List<IJsonTypeInfoResolver> currentActive = ActiveJsonTypeInfoResolvers;

            int targetCount = (JsonTypeInfoResolvers?.Length ?? 0) + (jsonTypeInfoResolversExtended?.Length ?? 0) + (RuntimeFeature.IsDynamicCodeSupported ? 1 : 0);

            if (currentActive == null || currentActive.Count < targetCount) {
                lock (_resolverLock) {
                    if (ActiveJsonTypeInfoResolvers == null || ActiveJsonTypeInfoResolvers.Count < targetCount) {
                        var tempResolvers = new List<IJsonTypeInfoResolver>(JsonTypeInfoResolvers);

                        if (jsonTypeInfoResolversExtended != null) {
                            tempResolvers.AddRange(jsonTypeInfoResolversExtended);
                        }

                        if (RuntimeFeature.IsDynamicCodeSupported) {
                            tempResolvers.Add(new DefaultJsonTypeInfoResolver());
                        }

                        ActiveJsonTypeInfoResolvers = tempResolvers;
                    }

                    currentActive = ActiveJsonTypeInfoResolvers;
                }
            }

            options.TypeInfoResolverChain.Clear();
            options.TypeInfoResolverChain.Add(new AutoRegisterTypeInfoResolver());

            var finalResolvers = new List<IJsonTypeInfoResolver>();

            if (options.TypeInfoResolver != null) {
                finalResolvers.Add(options.TypeInfoResolver);
            }

            finalResolvers.AddRange(currentActive);

            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine([.. finalResolvers]);
            options.TypeInfoResolverChain.Add(new JsonSerDeTypeInfoResolver(combined));

            return options;
        }

        public static IServiceCollection ConfigureHttpJsonOptionsEx(this IServiceCollection services, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended) {
            _ = ConfigureJson(null, jsonTypeInfoResolversExtended);

            return services.ConfigureHttpJsonOptions(options => {
                _ = ConfigureJson(options.SerializerOptions, jsonTypeInfoResolversExtended);
            });
        }

    }

}