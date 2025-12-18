using bifeldy_lib_90.Transformers;
using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace bifeldy_lib_90.Extensions {

    public static class TBuilderExtensions {

        public static TBuilder WithGroupNames<TBuilder>(this TBuilder builder, params string[] documents) where TBuilder : IEndpointConventionBuilder {
            List<string> docs = ["latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty)];
            if (documents != null) {
                foreach (string document in documents) {
                    if (!docs.Contains(document)) {
                        docs.Add(document);
                    }
                }
            }

            return builder.WithMetadata(new OpenApiGroupNames([.. docs]));
        }

    }

}
