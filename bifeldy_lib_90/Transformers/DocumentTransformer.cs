using bifeldy_lib_90.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

namespace bifeldy_lib_90.Transformers {

    public sealed record DocumentOptions(
        string Title,
        string Description,
        bool EnableApiKey,
        bool EnableJwt
    );

    public sealed record OpenApiGroupNames(params string[] Names);

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
            IHttpContextAccessor httpContextAccessor = context.ApplicationServices.GetService<IHttpContextAccessor>();
            HttpContext httpContext = httpContextAccessor?.HttpContext;

            if (httpContext != null) {
                string scheme = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpContext.Request.Scheme;
                string host = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpContext.Request.Host.Value;

                string pathBase = string.Empty;
                if (httpContext.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues proxyPath)) {
                    string p = proxyPath.Last()?.Trim('/');
                    if (!string.IsNullOrEmpty(p)) {
                        pathBase = $"/{p}";
                    }
                }

                document.Servers = [
                    new() {
                        Url = $"{scheme}://{host}{pathBase}",
                        Description = "Production Server (Proxy)"
                    }
                ];
            }

            string currentDocumentName = context.DocumentName;

            var filteredPaths = new OpenApiPaths();
            List<ApiDescription> allApis = [.. context.DescriptionGroups.SelectMany(g => g.Items)];

            foreach ((string path, OpenApiPathItem pathItem) in document.Paths) {
                var newPathItem = new OpenApiPathItem();

                foreach ((OperationType method, OpenApiOperation operation) in pathItem.Operations) {
                    bool include = true;

                    ApiDescription apiDesc = allApis.FirstOrDefault(d =>
                        d.HttpMethod?.Equals(method.ToString(), StringComparison.OrdinalIgnoreCase) == true &&
                        string.Equals(
                            "/" + d.RelativePath?.TrimEnd('/'),
                            path.TrimEnd('/'),
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                    if (apiDesc != null) {
                        OpenApiGroupNames groupMeta = apiDesc.ActionDescriptor
                            .EndpointMetadata
                            .OfType<OpenApiGroupNames>()
                            .FirstOrDefault();

                        if (groupMeta != null) {
                            include = groupMeta.Names
                                .Contains(currentDocumentName, StringComparer.OrdinalIgnoreCase);
                        }
                    }

                    if (include) {
                        newPathItem.Operations[method] = operation;
                    }
                }

                if (newPathItem.Operations.Count > 0) {
                    filteredPaths[path] = newPathItem;
                }
            }

            document.Paths = filteredPaths;
            document.Info.Title = this._opt.Title;
            document.Info.Description = this._opt.Description;
            document.Tags ??= [];

            var tagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ApiDescription api in allApis) {
                foreach (ApiTagDescription meta in api.ActionDescriptor.EndpointMetadata.OfType<ApiTagDescription>()) {
                    if (!string.IsNullOrEmpty(meta.Description)) {
                        tagMap[meta.Tag] = meta.Description;
                        break;
                    }
                }
            }

            foreach ((string tag, string desc) in tagMap) {
                OpenApiTag openApiTag = document.Tags.FirstOrDefault(t => t.Name == tag);

                if (openApiTag != null) {
                    openApiTag.Description = desc;
                }
                else {
                    openApiTag = new OpenApiTag() {
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

            var usedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (OpenApiPathItem pathItem in document.Paths.Values) {
                foreach (OpenApiOperation operation in pathItem.Operations.Values) {
                    if (operation.Tags is null) {
                        continue;
                    }

                    foreach (OpenApiTag tag in operation.Tags) {
                        if (!string.IsNullOrWhiteSpace(tag.Name)) {
                            _ = usedTags.Add(tag.Name);
                        }
                    }
                }
            }

            document.Tags = [.. document.Tags.Where(t => usedTags.Contains(t.Name))];

            return Task.CompletedTask;
        }

    }

}