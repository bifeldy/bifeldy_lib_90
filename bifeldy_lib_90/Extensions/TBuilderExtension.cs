using bifeldy_lib_90.Attributes;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Transformers;
using Microsoft.AspNetCore.Builder;
using System.Text.RegularExpressions;

namespace bifeldy_lib_90.Extensions {

    public static class TBuilderExtensions {

        public static TBuilder WithApiDocumentNames<TBuilder>(this TBuilder builder, params string[] documents) where TBuilder : IEndpointConventionBuilder {
            List<string> docs = [ApiDocumentName._ALL_];

            if (documents != null) {
                foreach (string document in documents) {
                    string doc = Regex.Replace(document, "[^a-zA-Z0-9_-]+", string.Empty);

                    if (!docs.Contains(doc)) {
                        if (!Bifeldy.OPEN_API_DOCUMENTS.Contains(doc)) {
                            throw new Exception("Nama Dokumen Tidak Tersedia");
                        }

                        docs.Add(doc);
                    }
                }
            }

            return builder.WithMetadata(new OpenApiGroupNames([.. docs]));
        }

        public static TBuilder WithAllowedRoles<TBuilder>(this TBuilder builder, params ESessionRole[] roles) where TBuilder : IEndpointConventionBuilder {
            return builder.WithMetadata(new AllowedRolesAttribute(roles));
        }

        public static TBuilder WithMinRole<TBuilder>(this TBuilder builder, ESessionRole role) where TBuilder : IEndpointConventionBuilder {
            return builder.WithMetadata(new MinRoleAttribute(role));
        }

        public static TBuilder WithRouteExclude<TBuilder>(this TBuilder builder, params DenyAccessAttribute[] routeExclude) where TBuilder : IEndpointConventionBuilder {
            foreach (DenyAccessAttribute re in routeExclude) {
                _ = builder.WithMetadata(re);
            }

            return builder;
        }

    }

}
