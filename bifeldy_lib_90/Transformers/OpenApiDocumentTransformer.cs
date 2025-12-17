using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace bifeldy_lib_90.Transformers {

    public sealed record OpenApiDocumentOptions(
        string Title,
        string Description,
        bool EnableApiKey,
        bool EnableJwt
    );

    public sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer {

        private readonly OpenApiDocumentOptions _opt;

        public OpenApiDocumentTransformer(OpenApiDocumentOptions opt) {
            this._opt = opt;
        }

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken
        ) {
            document.Info.Title = this._opt.Title;
            document.Info.Description = this._opt.Description;

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
