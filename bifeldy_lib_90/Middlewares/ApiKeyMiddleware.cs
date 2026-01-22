using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bifeldy_lib_90.Middlewares {

    public sealed class ApiKeyMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;

        public ApiKeyMiddleware(
            RequestDelegate next,
            ILogger<ApiKeyMiddleware> logger,
            IApplicationService app,
            IGlobalService gs,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._app = app;
            this._gs = gs;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IPostgres _pg, IApiKeyRepository _akRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            if (string.IsNullOrEmpty(apiPathRequested)) {
                await this._next(context);
                return;
            }

            Endpoint endpoint = context.GetEndpoint();
            IAllowAnonymous allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>();

            bool isApi = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.OrdinalIgnoreCase);

            string secret = context.Items["secret"]?.ToString();
            bool haveSecret = !string.IsNullOrEmpty(secret);

            if (!isApi || haveSecret || allowAnonymous != null) {
                await this._next(context);
                return;
            }

            string[] serverIps = this._app.GetAllIpAddress();
            foreach (string ip in serverIps) {
                if (!this._gs.AllowedIpOrigin.Contains(ip)) {
                    this._gs.AllowedIpOrigin.Add(ip);
                }
            }

            string ipDomainHost = request.Host.Host;
            if (!this._gs.AllowedIpOrigin.Contains(ipDomainHost)) {
                this._gs.AllowedIpOrigin.Add(ipDomainHost);
            }

            string ipDomainProxy = request.Headers["x-forwarded-host"];
            if (!string.IsNullOrEmpty(ipDomainProxy) && !this._gs.AllowedIpOrigin.Contains(ipDomainProxy)) {
                this._gs.AllowedIpOrigin.Add(ipDomainProxy);
            }

            string apiKey = context.Items["api_key"]?.ToString();
            string ipOrigin = context.Items["ip_origin"]?.ToString();

            this._logger.LogInformation("[KEY_IP_ORIGIN] 🌸 {apiKey} @ {ipOrigin}", apiKey, ipOrigin);

            // Khusus Bypass ~ Case Sensitive
            string hashText = this._chiper.HashText(this._app.AppName);
            if (apiKey == hashText || await _akRepo.CheckKeyOrigin(_pg, ipOrigin, apiKey)) {
                await this._next(context);
            }
            else {
                string errMsg = "Api Key Salah / Tidak Terdaftar!";

                response.Clear();
                response.StatusCode = StatusCodes.Status401Unauthorized;

                await response.WriteAsJsonAsync(
                    new ResponseJsonSingle<ResponseJsonErrorApiKeyIpOrigin>() {
                        info = $"{StatusCodes.Status401Unauthorized} - API Key :: Tidak Dapat Digunakan",
                        result = new ResponseJsonErrorApiKeyIpOrigin() {
                            message = errMsg,
                            api_key = apiKey,
                            ip_origin = ipOrigin
                        }
                    },
                    ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonErrorApiKeyIpOrigin
                );
            }
        }

    }

}