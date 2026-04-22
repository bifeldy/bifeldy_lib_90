using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace bifeldy_lib_90.Middlewares {

    public sealed class AutoCheckMultiDcMiddleware {

        private readonly EnvVar _env;

        private readonly RequestDelegate _next;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;

        public AutoCheckMultiDcMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            IApplicationService app,
            IGlobalService gs
        ) {
            this._next = next;
            this._env = env.Value;
            this._app = app;
            this._gs = gs;
        }

        public async Task Invoke(HttpContext context, IServerConfigRepository scr) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            try {
                string defaultAssetsFolder = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_ASSETS_FOLDER);

                if (context.Request.Path.Value.StartsWith("/server-config.html", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "html/server-config.html"));
                    return;
                }
                else if (context.Request.Path.Value.StartsWith("/css/bootstrap.min.css", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/css";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "css/bootstrap.min.css"));
                    return;
                }
                else if (context.Request.Path.Value.StartsWith("/js/bootstrap.bundle.min.js", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/javascript";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "js/bootstrap.bundle.min.js"));
                    return;
                }
                else if (context.Request.Path.Value.StartsWith("/img/domar.gif", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "image/gif";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "img/domar.gif"));
                    return;
                }
                else if (context.Request.Path.Value.StartsWith("/img/domar.ico", StringComparison.OrdinalIgnoreCase) || context.Request.Path.Value.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "image/x-icon";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "img/domar.ico"));
                    return;
                }
                else if (context.Request.Path.Value.StartsWith("/img/indomaret.png", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "image/png";
                    await context.Response.SendFileAsync(Path.Combine(defaultAssetsFolder, "img/indomaret.png"));
                    return;
                }

                int shortCircuit = 0;
                object res = null;

                if (context.Request.Path.Value.Equals("/api/server-config", StringComparison.OrdinalIgnoreCase)) {
                    try {
                        if (context.Request.Method == "GET") {
                            IEnumerable<ServerConfigKunci> config = await scr.GetKodeServerKunciDc();

                            shortCircuit = StatusCodes.Status200OK;
                            res = new ResponseJsonMulti<ServerConfigKunci>() {
                                info = $"{shortCircuit} - Kunci Kode DC",
                                results = config,
                                count = (ulong)config.Count(),
                                pages = 1
                            };
                        }
                        else {
                            ServerConfigAddEditDelete reqBody = await this._gs.GetHttpRequestBody(
                                context.Request,
                                ServerConfigJsonSerializerContext.Default.ServerConfigAddEditDelete
                            );

                            if (reqBody == null || string.IsNullOrEmpty(reqBody?.password)) {
                                throw new TidakMemenuhiException("Data Tidak Lengkap");
                            }

                            string info = null;
                            string message = null;

                            if (!reqBody.password.Equals("5p1nd0m@r3T", StringComparison.OrdinalIgnoreCase)) {
                                info = "Kunci Kode DC";
                                message = "Password Salah";
                                shortCircuit = StatusCodes.Status401Unauthorized;
                            }
                            else if (context.Request.Method == "POST" && reqBody != null) {
                                if (reqBody.type.ToUpper() == "TAMBAH") {
                                    _ = await scr.AddKodeServerKunciDc(reqBody.kode_dc, reqBody.kunci_gxxx, reqBody.server_target);
                                    info = "Kunci Kode DC";
                                    message = "Berhasil Menambah Kunci";
                                    shortCircuit = StatusCodes.Status201Created;
                                }
                                else if (reqBody.type.ToUpper() == "UBAH") {
                                    _ = await scr.EditKodeServerKunciDc(reqBody.kode_dc, reqBody.kunci_gxxx, reqBody.server_target);
                                    info = "Kunci Kode DC";
                                    message = "Berhasil Mengubah Kunci";
                                    shortCircuit = StatusCodes.Status202Accepted;
                                }
                                else if (reqBody.type.ToUpper() == "HAPUS") {
                                    _ = await scr.RemoveKodeServerKunciDc(reqBody.kode_dc);
                                    info = "Kunci Kode DC";
                                    message = "Berhasil Menghapus Kunci";
                                    shortCircuit = StatusCodes.Status202Accepted;
                                }

                                // TODO :: New Features ~
                            }

                            if (string.IsNullOrEmpty(info) || string.IsNullOrEmpty(message)) {
                                throw new TidakMemenuhiException("Data Tidak Lengkap");
                            }

                            res = new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = $"{shortCircuit} - {info}",
                                result = new ResponseJsonMessage() {
                                    message = message
                                }
                            };
                        }
                    }
                    catch (TidakMemenuhiException e) {
                        shortCircuit = StatusCodes.Status400BadRequest;
                        res = new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"{shortCircuit} - Kunci Kode DC",
                            result = new ResponseJsonMessage() {
                                message = e.Message
                            }
                        };
                    }
                    catch (Exception e) {
                        shortCircuit = StatusCodes.Status500InternalServerError;
                        res = new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"{shortCircuit} - Whoops :: Terjadi Kesalahan",
                            result = new ResponseJsonMessage() {
                                message = this._app.DebugMode ? e.Message : "Gagal Melanjutkan Permintaan"
                            }
                        };
                    }
                }

                if (shortCircuit > 0 && res != null) {
                    context.Response.StatusCode = shortCircuit;
                    if (context.Response.StatusCode == StatusCodes.Status200OK) {
                        await context.Response.WriteAsJsonAsync(res, ServerConfigJsonSerializerContext.Default.ResponseJsonMultiServerConfigKunci);
                    }
                    else {
                        await context.Response.WriteAsJsonAsync(res, ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage);
                    }

                    return;
                }

                string kunciGxxx = this._env.KUNCI_GXXX;
                if (!string.IsNullOrEmpty(kunciGxxx)) {
                    if (kunciGxxx.StartsWith("/")) {
                        if (context.Request.Headers.ContainsKey(Bifeldy.NGINX_PATH_NAME)) {
                            _ = context.Request.Headers.Remove(Bifeldy.NGINX_PATH_NAME);
                        }

                        context.Request.Headers.Append(Bifeldy.NGINX_PATH_NAME, kunciGxxx);
                    }
                }

                context.Items["kunci_gxxx"] = scr.CurrentLoadedKodeServerKunciDc();

                await this._next(context);
            }
            catch (KunciServerTidakTersediaException ex) {
                string redirectUrl = "/server-config.html";
                string encodedString = WebUtility.UrlEncode(ex.Message);

                if (!this._app.DebugMode && context.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                    string proxyPath = pathBase.Last();
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        redirectUrl = $"{proxyPath}{redirectUrl}";
                    }
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = $"{redirectUrl}?errorInfo={encodedString}";
            }
        }

    }

}