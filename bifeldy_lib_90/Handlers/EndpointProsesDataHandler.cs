using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Web;

namespace bifeldy_lib_90.Handlers {

    public interface IEndpointProsesDataHandler : IEndpointBaseHandler {
        Task<IResult> HitDimanaSaja<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new();
        Task<IResult> HitDc<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new();
        Task<IResult> DirectDbDc<TFormDataStream>(IDatabase db, IServiceProvider sp, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = "(Mirror)", [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new();
        Task<IResult> HitHo<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new();
        Task<IResult> HitNonDc<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new();
    }

    public sealed class CEndpointProsesDataHandler : CEndpointBaseHandler, IEndpointProsesDataHandler {

        public CEndpointProsesDataHandler(
            CronScheduler scheduler,
            //
            ILogger<CEndpointProsesDataHandler> logger,
            IServiceProvider sp,
            IGlobalService gs,
            IApplicationService app,
            IHttpService http,
            IConverterService cs,
            IRdlcService rs,
            IGeneralRepository generalRepo
        ) : base(scheduler, logger, sp, gs, app, http, cs, rs, generalRepo) {
            //
        }

        public async Task<IResult> HitDimanaSaja<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new() {
            customService ??= (IServiceProsesDataHandler)this._baseService;

            // formDataStream :: Boleh NULL

            (int statusCode, ResponseJson res, JsonTypeInfo jti) = await customService.Run(this._context, db, formDataStream, jsonTypeInfo, callerMemberName);
            if (res == null) {
                return Results.Empty;
            }

            if (typeof(ResponseRedirect).IsAssignableFrom(res.GetType()) && statusCode >= 300 && statusCode < 400) {
                var redir = (ResponseRedirect)res;

                switch (statusCode) {
                    case StatusCodes.Status301MovedPermanently:
                        return Results.Redirect(redir.url, true, false);
                    case StatusCodes.Status302Found:
                        return Results.Redirect(redir.url, false, false);
                    case StatusCodes.Status307TemporaryRedirect:
                        return Results.Redirect(redir.url, false, true);
                    case StatusCodes.Status308PermanentRedirect:
                        return Results.Redirect(redir.url, true, true);

                    case StatusCodes.Status306SwitchProxy:
                        string currentQueryString = this._context.Request.QueryString.ToString();
                        NameValueCollection queryString = HttpUtility.ParseQueryString(currentQueryString);

                        var uriBuilder = new UriBuilder(redir.url);
                        NameValueCollection uriBuilderQuery = HttpUtility.ParseQueryString(uriBuilder.Query);

                        if (uriBuilderQuery != null) {
                            foreach (string key in uriBuilderQuery.Keys) {
                                queryString.Set(key, uriBuilderQuery[key]);
                            }
                        }

                        uriBuilder.Query = queryString.ToString();

                        Uri u = uriBuilder.Uri;

                        bool isNotStream = typeof(InputJsonDc).IsAssignableFrom(formDataStream.GetType());
                        if (isNotStream) {
                            string jsonBody = this._cs.ObjectToJson((TFormDataStream)formDataStream, jsonTypeInfo);
                            await using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))) {
                                this._context.Request.Body = ms;
                                return await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true);
                            }
                        }

                        return await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true);
                }
            }

            res.info = $"{statusCode} - {callerMemberName}";
            if (!string.IsNullOrEmpty(customInfo)) {
                res.info += $" {customInfo}";
            }

            return Results.Json(
                res, jti,
                MediaTypeNames.Application.Json,
                statusCode
            );
        }

        /* ** *** ** */

        public async Task<IResult> HitDc<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new() {
            try {
                customService ??= (IServiceProsesDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (isNonDc) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui HO / DC"
                        }
                    });
                }

                var f = new InputJsonDc();
                bool isNotStream = typeof(InputJsonDc).IsAssignableFrom(formDataStream.GetType());

                if (isNotStream) {
                    f = (InputJsonDc)formDataStream;
                }
                else if (!string.IsNullOrEmpty(this._context.Request.Headers["x-dc"])) {
                    f.kode_dc = this._context.Request.Headers["x-dc"];
                }
                else if (!string.IsNullOrEmpty(this._context.Request.Query["dc"])) {
                    f.kode_dc = this._context.Request.Query["dc"];
                }

                if (formDataStream == null || string.IsNullOrEmpty(f.kode_dc)) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Data Tidak Lengkap!"
                        }
                    });
                }

                IResult er = await this.CheckExcludeJenisDcWithResult(db, f.kode_dc, callerMemberName);
                if (er != null) {
                    return er;
                }

                bool isHo = await this._generalRepo.IsHo(db);
                if (!isHo) {
                    return await this.HitDimanaSaja(db, formDataStream, jsonTypeInfo, customService, customInfo, callerMemberName);
                }
                else {
                    await this._generalRepo.CheckKoordinatorHO(db, f.kode_dc);

                    string e = null;
                    Uri u = null;
                    await this._generalRepo.GetDcApiPathAppFromHo(db, this._context.Request, f.kode_dc, (err, res) => {
                        e = err;
                        u = res;
                    });

                    if (u == null) {
                        return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"400 - {callerMemberName}",
                            result = new ResponseJsonMessage() {
                                message = e
                            }
                        });
                    }

                    if (isNotStream) {
                        NameValueCollection queryDictionary = HttpUtility.ParseQueryString(u.Query);

                        f.secret = queryDictionary["secret"];
                        f.key = queryDictionary["key"];
                        f.token = queryDictionary["token"];

                        string jsonBody = this._cs.ObjectToJson((TFormDataStream)(object)f, jsonTypeInfo);
                        await using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))) {
                            this._context.Request.Body = ms;
                            return await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true);
                        }
                    }

                    return await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true);
                }
            }
            catch (TidakMemenuhiException e) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = e.Message
                    }
                });
            }
        }

        /* ** ALTERNATIVE DC API ** */

        public async Task<IResult> DirectDbDc<TFormDataStream>(IDatabase db, IServiceProvider sp, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = "(Mirror)", [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new() {
            try {
                customService ??= (IServiceProsesDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                bool isHo = await this._generalRepo.IsHo(db);

                if (isNonDc || !isHo) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName} (Mirror)",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui HO"
                        }
                    });
                }

                var f = new InputJsonDc();
                bool isNotStream = typeof(InputJsonDc).IsAssignableFrom(formDataStream.GetType());

                if (isNotStream) {
                    f = (InputJsonDc)formDataStream;
                }
                else if (!string.IsNullOrEmpty(this._context.Request.Headers["x-dc"])) {
                    f.kode_dc = this._context.Request.Headers["x-dc"];
                }
                else if (!string.IsNullOrEmpty(this._context.Request.Query["dc"])) {
                    f.kode_dc = this._context.Request.Query["dc"];
                }

                if (formDataStream == null || string.IsNullOrEmpty(f.kode_dc)) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName} (Mirror)",
                        result = new ResponseJsonMessage() {
                            message = "Data Tidak Lengkap!"
                        }
                    });
                }

                IResult er = await this.CheckExcludeJenisDcWithResult(db, f.kode_dc, callerMemberName);
                if (er != null) {
                    return er;
                }

                await this._generalRepo.CheckKoordinatorHO(db, f.kode_dc);

                (IDatabase dbPg, IDatabase dbMsSql) = await this._generalRepo.OpenConnectionToDcFromHo(db, f.kode_dc, sp);
                if (dbPg == null) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName} (Mirror)",
                        result = new ResponseJsonMessage() {
                            message = $"Kode gudang ({f.kode_dc}) tidak tersedia!"
                        }
                    });
                }

                return await this.HitDimanaSaja(dbPg, formDataStream, jsonTypeInfo, customService, customInfo, callerMemberName);
            }
            catch (TidakMemenuhiException e) {
                var res = new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = e.Message
                    }
                };

                if (!string.IsNullOrEmpty(customInfo)) {
                    res.info += $" {customInfo}";
                }

                return Results.BadRequest(res);
            }
        }

        /* ** *** ** */

        public async Task<IResult> HitHo<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new() {
            try {
                customService ??= (IServiceProsesDataHandler)this._baseService;

                bool isHo = await this._generalRepo.IsHo(db);
                if (!isHo) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui HO"
                        }
                    });
                }

                return await this.HitDimanaSaja(db, formDataStream, jsonTypeInfo, customService, customInfo, callerMemberName);
            }
            catch (TidakMemenuhiException e) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = e.Message
                    }
                });
            }
        }

        public async Task<IResult> HitNonDc<TFormDataStream>(IDatabase db, object formDataStream = null, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, IServiceProsesDataHandler customService = null, string customInfo = null, [CallerMemberName] string callerMemberName = null) where TFormDataStream : JsonSerDe, new() {
            try {
                customService ??= (IServiceProsesDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (!isNonDc) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui NON DC"
                        }
                    });
                }

                return await this.HitDimanaSaja(db, formDataStream, jsonTypeInfo, customService, customInfo, callerMemberName);
            }
            catch (TidakMemenuhiException e) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = e.Message
                    }
                });
            }
        }

    }
}
