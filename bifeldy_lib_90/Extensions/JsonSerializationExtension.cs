using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Resolvers;
using bifeldy_lib_90.TableView;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class JsonSerializationExtension {

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

        private static readonly Lock _resolverLock = new();
        private static JsonSerializerOptions _jsonSerializerOptions = null;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safety guaranteed by JsonTypeInfo usage.")]
        public static JsonSerializerOptions ConfigureJson(JsonSerializerOptions options = null, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended = null) {
            if (options == null) {
                if (_jsonSerializerOptions == null) {
                    lock (_resolverLock) {
                        if (_jsonSerializerOptions == null) {
                            var baseOptions = new JsonSerializerOptions() {
                                PropertyNamingPolicy = null,
                                PropertyNameCaseInsensitive = true,
                                TypeInfoResolver = JsonTypeInfoResolver.Combine([.. JsonTypeInfoResolvers])
                            };

                            baseOptions.Converters.Add(new DecimalConverter());
                            baseOptions.Converters.Add(new NullableDecimalConverter());

                            _jsonSerializerOptions = baseOptions;
                        }
                    }
                }

                if (jsonTypeInfoResolversExtended == null || jsonTypeInfoResolversExtended.Length == 0) {
                    return _jsonSerializerOptions;
                }
            }

            JsonSerializerOptions targetOptions = options ?? _jsonSerializerOptions;

            targetOptions.PropertyNamingPolicy = null;
            targetOptions.PropertyNameCaseInsensitive = true;

            if (!targetOptions.Converters.Any(c => c is DecimalConverter)) {
                targetOptions.Converters.Add(new DecimalConverter());
            }

            if (!targetOptions.Converters.Any(c => c is NullableDecimalConverter)) {
                targetOptions.Converters.Add(new NullableDecimalConverter());
            }

            bool hasAuto = targetOptions.TypeInfoResolverChain.Any(tir => tir is AutoRegisterTypeInfoResolver);
            bool hasJson = targetOptions.TypeInfoResolverChain.Any(tir => tir is JsonSerDeTypeInfoResolver);

            if (!hasAuto || !hasJson || (jsonTypeInfoResolversExtended != null && jsonTypeInfoResolversExtended.Length > 0)) {
                targetOptions.TypeInfoResolverChain.Clear();
                targetOptions.TypeInfoResolverChain.Add(new AutoRegisterTypeInfoResolver());

                var finalResolvers = new List<IJsonTypeInfoResolver>();

                if (targetOptions.TypeInfoResolver != null) {
                    finalResolvers.Add(targetOptions.TypeInfoResolver);
                }

                if (JsonTypeInfoResolvers != null) {
                    finalResolvers.AddRange(JsonTypeInfoResolvers);
                }

                if (jsonTypeInfoResolversExtended != null) {
                    finalResolvers.AddRange(jsonTypeInfoResolversExtended);
                }

                if (RuntimeFeature.IsDynamicCodeSupported) {
                    finalResolvers.Add(new DefaultJsonTypeInfoResolver());
                }

                IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine([.. finalResolvers]);
                targetOptions.TypeInfoResolverChain.Add(new JsonSerDeTypeInfoResolver(combined));
            }

            return targetOptions;
        }

        public static IServiceCollection ConfigureHttpJsonOptionsEx(this IServiceCollection services, IJsonTypeInfoResolver[] jsonTypeInfoResolversExtended = null) {
            _ = ConfigureJson(null, jsonTypeInfoResolversExtended);

            _ = services.Configure<JsonOptions>(options => ConfigureJson(options.SerializerOptions, jsonTypeInfoResolversExtended));
            _ = services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = long.MaxValue);

            return services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions, jsonTypeInfoResolversExtended));
        }

    }

}
