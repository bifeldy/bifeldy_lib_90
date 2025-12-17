using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace bifeldy_lib_90.Middlewares {

    public sealed class SecretMiddleware {

        private readonly EnvVar _env;

        private readonly RequestDelegate _next;
        private readonly ILogger<SecretMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public SecretMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            ILogger<SecretMiddleware> logger,
            IApplicationService app,
            IChiperService chiper
        ) {
            this._next = next;
            this._env = env.Value;
            this._logger = logger;
            this._app = app;
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

            bool isApi = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase);
            if (!isApi) {
                await this._next(context);
                return;
            }

            string secret = context.Items["secret"]?.ToString();

            this._logger.LogInformation("[SECRET_MIDDLEWARE] 🗝 {secret}", secret);

            if (!string.IsNullOrEmpty(secret)) {
                bool allowed = false;

                // Khusus Bypass ~ Case Sensitive
                string hashText = this._chiper.HashText(this._app.AppName);
                if (secret == hashText || await _akRepo.SecretLogin(this._env.IS_USING_POSTGRES, _pg, secret) != null) {
                    allowed = true;
                }

                if (!allowed) {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;

                    await response.WriteAsJsonAsync(
                        new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "401 - Secret :: Tidak Dapat Digunakan",
                            result = new ResponseJsonMessage() {
                                message = "Secret salah / tidak dikenali!"
                            }
                        },
                        ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                    );

                    return;
                }

                string token = context.Items["token"]?.ToString();
                if (string.IsNullOrEmpty(token)) {
                    string addrIp = context.Items["address_ip"]?.ToString();

                    if (request.Query.ContainsKey("mask_ip")) {
                        addrIp = this._chiper.DecryptText(request.Query["mask_ip"], hashText);
                    }

                    string addrOrigin = context.Items["address_origin"]?.ToString();
                    string ipOrigin = addrOrigin == addrIp ? addrOrigin : $"{addrOrigin}@{addrIp}";
                    context.Items["ip_origin"] = ipOrigin;

                    var userSession = new UserApiSession() {
                        name = addrIp,
                        role = UserSessionRole.PROGRAM_SERVICE
                    };

                    IEnumerable<Claim> claims = new List<Claim>() {
                        new(ClaimTypes.Name, userSession.name),
                        new(ClaimTypes.Role, userSession.role.ToString())
                    };

                    token = this._chiper.EncodeJWT(claims);

                    context.Items["token"] = token;
                }
            }

            await this._next(context);
        }

    }

}