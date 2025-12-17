using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;

namespace bifeldy_lib_90.Middlewares {

    public sealed class JwtMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public JwtMiddleware(
            RequestDelegate next,
            ILogger<JwtMiddleware> logger,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context) {
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

            bool isApi = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase);
            if (!isApi || allowAnonymous != null) {
                await this._next(context);
                return;
            }

            string token = context.Items["token"]?.ToString();

            this._logger.LogInformation("[JWT_MIDDLEWARE] 🔐 {token}", token);

            context.Items["user"] = null;

            if (!string.IsNullOrEmpty(token)) {
                try {
                    IEnumerable<Claim> userClaim = this._chiper.DecodeJWT(token);

                    var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                    context.User = new ClaimsPrincipal(userClaimIdentity);

                    Claim _claimName = userClaim.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault();
                    Claim _claimRole = userClaim.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault();
                    if (_claimName == null || _claimRole == null) {
                        throw new Exception("Format Token Salah / Expired!");
                    }

                    var userInfo = new UserApiSession() {
                        name = _claimName.Value,
                        role = (UserSessionRole)Enum.Parse(typeof(UserSessionRole), _claimRole.Value)
                    };

                    context.Items["user"] = userInfo;
                }
                catch {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;

                    await response.WriteAsJsonAsync(
                        new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "401 - JWT :: Tidak Dapat Digunakan",
                            result = new ResponseJsonMessage() {
                                message = "Format Token Salah / Expired!"
                            }
                        },
                        ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                    );

                    return;
                }
            }

            await this._next(context);
        }

    }

}