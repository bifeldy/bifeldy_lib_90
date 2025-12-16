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
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["api_key"] =
                    new OpenApiSecurityScheme {
                        Type = SecuritySchemeType.ApiKey,
                        Name = "key",
                        In = ParameterLocation.Query
                    };
            }

            if (this._opt.EnableJwt) {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["jwt"] =
                    new OpenApiSecurityScheme {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer"
                    };
            }

            return Task.CompletedTask;
        }

    }

}
