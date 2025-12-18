using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using System.Security.Claims;

namespace bifeldy_lib_90.Endpoints {

    public static class DefaultEnpoint {

        [UnconditionalSuppressMessage(
            "Trimming", "IL2026",
            Justification = "Minimal API handler is static and AOT-safe"
        )]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050",
            Justification = "Minimal API handler uses static delegate with known types"
        )]
        public static void MapDefaultEndpoints(this RouteGroupBuilder routeGroupBuilder) {
            string documentName = "latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty);
            RouteGroupBuilder todoApi = routeGroupBuilder.MapGroupTagDescription("/", "_", "Fitur standar bawaan ~")
                .WithGroupNames(documentName);

            _ = todoApi.MapPost("/login", Login)
                .WithSummary("Login")
                .WithDescription("Ambil token untuk 1 jam kedepan")
                .Accepts<LoginInfo>(MediaTypeNames.Application.Json)
                .Produces<ResponseJsonSingle<ResponseJsonMessage>>(StatusCodes.Status400BadRequest)
                .Produces<ResponseJsonSingle<string>>(StatusCodes.Status201Created)
                .AllowAnonymous();
        }

        private static async Task<IResult> Login(
            [FromBody] LoginInfo reqBody,
            HttpContext _httpContext,
            IOptions<EnvVar> _envVar,
            IChiperService _chiper,
            IPostgres _pg,
            IApiKeyRepository _apiKeyRepo,
            IApiTokenRepository _apiTokenRepo,
            IUserRepository _userRepo
        ) {
            string userName = reqBody?.user_name;
            string password = reqBody?.password;
            string secret = reqBody?.secret;

            if (string.IsNullOrEmpty(secret) && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "400 - Login Gagal",
                    result = new ResponseJsonMessage() {
                        message = "Data Tidak Lengkap!"
                    }
                });
            }

            JwtSession userSession = null;

            if (string.IsNullOrEmpty(secret)) {
                ESessionRole userRole = default;

                API_TOKEN_T apiTokenT = await _apiTokenRepo.LoginBot(_pg, userName, password);
                if (apiTokenT == null) {
                    DC_USER_T dcUserT = await _userRepo.GetByUserNameNikPassword(_pg, userName, password);
                    if (dcUserT == null) {
                        return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "400 - Login Gagal",
                            result = new ResponseJsonMessage() {
                                message = "User name / password salah!"
                            }
                        });
                    }
                    else {
                        userRole = ESessionRole.USER_SD_SSD_3;
                    }
                }
                else {
                    userRole = ESessionRole.EXTERNAL_BOT;
                }

                userSession = new JwtSession {
                    name = userName.ToUpper(),
                    role = userRole
                };
            }
            else {
                API_KEY_T apiKeyT = await _apiKeyRepo.SecretLogin(_pg, secret);
                if (apiKeyT == null) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = "400 - Login Gagal",
                        result = new ResponseJsonMessage() {
                            message = "Secret salah / tidak dikenali!"
                        }
                    });
                }
                else {
                    userSession = new JwtSession {
                        name = _httpContext.Items["address_ip"].ToString(),
                        role = ESessionRole.PROGRAM_SERVICE
                    };
                }
            }

            IEnumerable<Claim> claims = new List<Claim>() {
                new(ClaimTypes.Name, userSession.name),
                new(ClaimTypes.Role, userSession.role.ToString())
            };

            string token = _chiper.EncodeJWT(claims);

            return Results.Ok(new ResponseJsonSingle<string>() {
                info = $"{StatusCodes.Status201Created} - Login",
                result = token
            });
        }

    }

}
