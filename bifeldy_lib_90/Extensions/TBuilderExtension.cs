using bifeldy_lib_90.Transformers;
using Microsoft.AspNetCore.Builder;
using System.Reflection;
using System.Text.RegularExpressions;

namespace bifeldy_lib_90.Extensions {

    public static class TBuilderExtensions {

        public static TBuilder WithGroupNames<TBuilder>(this TBuilder builder, params string[] documents) where TBuilder : IEndpointConventionBuilder {
            List<string> docs = ["latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty)];

            if (documents != null) {
                foreach (string document in documents) {
                    string doc = Regex.Replace(document, "[^a-zA-Z0-9_-]+", string.Empty);

                    if (!docs.Contains(doc)) {
                        docs.Add(doc);
                    }
                }
            }

            return builder.WithMetadata(new OpenApiGroupNames([.. docs]));
        }

    }

}
