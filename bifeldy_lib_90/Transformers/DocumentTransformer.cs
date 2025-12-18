using bifeldy_lib_90.Extensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace bifeldy_lib_90.Transformers {

    public sealed record DocumentOptions(
        string Title,
        string Description,
        bool EnableApiKey,
        bool EnableJwt
    );

    public sealed class DocumentTransformer : IOpenApiDocumentTransformer {

        private readonly DocumentOptions _opt;

        public DocumentTransformer(DocumentOptions opt) {
            this._opt = opt;
        }

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken
        ) {
            document.Info.Title = this._opt.Title;
            document.Info.Description = this._opt.Description;
            document.Tags ??= [];

            var tagMap = new Dictionary<string, string>();
            foreach (ApiDescriptionGroup group in context.DescriptionGroups) {
                foreach (ApiDescription api in group.Items) {
                    foreach (ApiTagDescription meta in api.ActionDescriptor.EndpointMetadata.OfType<ApiTagDescription>()) {
                        if (!string.IsNullOrEmpty(meta.Description)) {
                            tagMap[meta.Tag] = meta.Description;
                            break;
                        }
                    }
                }
            }

            foreach ((string tag, string desc) in tagMap) {
                OpenApiTag openApiTag = document.Tags.FirstOrDefault(t => t.Name == tag);

                if (openApiTag != null) {
                    openApiTag.Description = desc;
                }
                else {
                    openApiTag = new OpenApiTag {
                        Name = tag,
                        Description = desc
                    };

                    document.Tags.Add(openApiTag);
                }
            }

            if (this._opt.EnableApiKey) {
                var apiKey = new OpenApiSecurityScheme() {
                    Description = @"API-Key Origin. Example: 'http://.../...?key=000...'",
                    Name = "key",
                    In = ParameterLocation.Query,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKey",
                    Reference = new OpenApiReference() {
                        Id = "api_key",
                        Type = ReferenceType.SecurityScheme
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes.Add(apiKey.Reference.Id, apiKey);
                document.SecurityRequirements.Add(new OpenApiSecurityRequirement() {
                    { apiKey, Array.Empty<string>() }
                });
            }

            if (this._opt.EnableJwt) {
                var jwt = new OpenApiSecurityScheme() {
                    Description = @"Authorization Header. Example: 'Bearer eyj...'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Reference = new OpenApiReference() {
                        Id = "jwt",
                        Type = ReferenceType.SecurityScheme
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes.Add(jwt.Reference.Id, jwt);

                document.SecurityRequirements.Add(new OpenApiSecurityRequirement() {
                    { jwt, Array.Empty<string>() }
                });
            }

            return Task.CompletedTask;
        }

    }

}
