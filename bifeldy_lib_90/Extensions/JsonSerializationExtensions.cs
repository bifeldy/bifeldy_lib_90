using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class JsonSerializationExtensions {

        public static IJsonTypeInfoResolver[] JsonTypeInfoResolvers = {
            // TODO: Add additional JsonSerializerContext generated resolvers here
        };

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
