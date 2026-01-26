using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Attributes;
using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Handlers {

    public interface IEndpointBaseHandler {
        CEndpointBaseHandler SetHttpContext(HttpContext context);
        CEndpointBaseHandler SetService(IServiceBaseHandler baseService);
        CEndpointBaseHandler Initialize(HttpContext context, IServiceBaseHandler baseService);
        IResult GetTableClassStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo, [CallerMemberName] string callerMemberName = null) where T : JsonSerDe, new();
        IResult GetPocoStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo, [CallerMemberName] string callerMemberName = null) where T : JsonSerDe, new();
    }

    public abstract class CEndpointBaseHandler : IEndpointBaseHandler {

        protected HttpContext _context;

        protected readonly CronScheduler _scheduler;
        //
        protected readonly ILogger<CEndpointBaseHandler> _logger;
        protected readonly IServiceProvider _sp;
        protected readonly IGlobalService _gs;
        protected readonly IApplicationService _app;
        protected readonly IHttpService _http;
        protected readonly IConverterService _cs;
        protected readonly IRdlcService _rs;
        protected readonly IGeneralRepository _generalRepo;

        //
        // Ini Nantinya Akan Ketimpa Sama Class Turunannya
        //
        protected IServiceBaseHandler _baseService;

        public CEndpointBaseHandler(
            CronScheduler scheduler,
            //
            ILogger<CEndpointBaseHandler> logger,
            IServiceProvider sp,
            IGlobalService gs,
            IApplicationService app,
            IHttpService http,
            IConverterService cs,
            IRdlcService rs,
            IGeneralRepository generalRepo
        ) {
            this._scheduler = scheduler;
            //
            this._logger = logger;
            this._sp = sp;
            this._gs = gs;
            this._app = app;
            this._http = http;
            this._cs = cs;
            this._rs = rs;
            this._generalRepo = generalRepo;
        }

        public CEndpointBaseHandler SetHttpContext(HttpContext context) {
            this._context = context;
            return this;
        }

        public CEndpointBaseHandler SetService(IServiceBaseHandler baseService) {
            this._baseService = baseService;
            return this;
        }

        public CEndpointBaseHandler Initialize(HttpContext context, IServiceBaseHandler baseService) {
            return this.SetHttpContext(context).SetService(baseService);
        }

        protected async Task<IResult> CheckExcludeJenisDcWithResult(IDatabase db, string kodeDc, [CallerMemberName] string callerMemberName = null) {
            string targetKodeDc = await this._generalRepo.GetKodeDc(db);
            EJenisDc targetJenisDc = await this._generalRepo.GetJenisDc(db);

            if (string.IsNullOrEmpty(kodeDc) && targetJenisDc != EJenisDc.HO && targetJenisDc != EJenisDc.NONDC) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{StatusCodes.Status400BadRequest} - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = "Data Tidak Lengkap!"
                    }
                });
            }

            string jenisDc = targetJenisDc.ToString();
            if (kodeDc != targetKodeDc) {
                jenisDc = await this._generalRepo.GetJenisDc(db, kodeDc);
            }

            if (Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                targetKodeDc = kodeDc;
                targetJenisDc = eJenisDc;
            }
            else {
                return Results.NotFound(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{StatusCodes.Status404NotFound} - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = $"Kode DC {kodeDc.ToUpper()} Tidak Tersedia"
                    }
                });
            }

            Endpoint endpoint = this._context.GetEndpoint();
            IEnumerable<object> attribs = endpoint?.Metadata
                .Where(t => typeof(DenyAccessAttribute).IsAssignableFrom(t.GetType()));

            bool isAllowed = true;
            foreach (DenyAccessAttribute attrib in attribs.Cast<DenyAccessAttribute>()) {
                isAllowed = this._gs.IsAllowedRoutingTarget(attrib.GetType(), targetKodeDc, targetJenisDc);

                if (!isAllowed) {
                    break;
                }
            }

            if (!isAllowed) {
                return Results.Json(
                    new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"{StatusCodes.Status403Forbidden} - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = $"Tidak Dapat Menggunakan DC :: `{targetJenisDc}` :: Karena Masuk Dalam Daftar Pengecualian"
                        }
                    },
                    ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                    MediaTypeNames.Application.Json,
                    StatusCodes.Status403Forbidden
                );
            }

            return null;
        }

        protected async Task<bool> CheckExcludeJenisDcNoResult(IDatabase db, string kodeDc, [CallerMemberName] string callerMemberName = null) {
            string targetKodeDc = await this._generalRepo.GetKodeDc(db);
            EJenisDc targetJenisDc = await this._generalRepo.GetJenisDc(db);

            if (string.IsNullOrEmpty(kodeDc) && targetJenisDc != EJenisDc.HO && targetJenisDc != EJenisDc.NONDC) {
                var response = new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{StatusCodes.Status400BadRequest} - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = "Data Tidak Lengkap!"
                    }
                };

                this._context.Response.StatusCode = StatusCodes.Status400BadRequest;
                this._context.Response.ContentType = MediaTypeNames.Application.Json;

                await JsonSerializer.SerializeAsync(
                    this._context.Response.Body,
                    response, ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                    _context.RequestAborted
                );

                return false;
            }
            else {
                string jenisDc = targetJenisDc.ToString();
                if (kodeDc != targetKodeDc) {
                    jenisDc = await this._generalRepo.GetJenisDc(db, kodeDc);
                }

                if (!Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                    var response = new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"{StatusCodes.Status404NotFound} - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = $"Kode DC {kodeDc.ToUpper()} Tidak Tersedia"
                        }
                    };

                    this._context.Response.StatusCode = StatusCodes.Status404NotFound;
                    this._context.Response.ContentType = MediaTypeNames.Application.Json;

                    await JsonSerializer.SerializeAsync(
                        this._context.Response.Body,
                        response, ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                        _context.RequestAborted
                    );

                    return false;
                }
                else {
                    targetKodeDc = kodeDc;
                    targetJenisDc = eJenisDc;

                    Endpoint endpoint = this._context.GetEndpoint();
                    IEnumerable<object> attribs = endpoint?.Metadata
                        .Where(t => typeof(DenyAccessAttribute).IsAssignableFrom(t.GetType()));

                    bool isAllowed = true;
                    foreach (DenyAccessAttribute attrib in attribs.Cast<DenyAccessAttribute>()) {
                        isAllowed = this._gs.IsAllowedRoutingTarget(attrib.GetType(), targetKodeDc, targetJenisDc);

                        if (!isAllowed) {
                            break;
                        }
                    }

                    if (!isAllowed) {
                        var response = new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"{StatusCodes.Status403Forbidden} - {callerMemberName}",
                            result = new ResponseJsonMessage() {
                                message = $"Tidak Dapat Menggunakan DC :: `{targetJenisDc}` :: Karena Masuk Dalam Daftar Pengecualian"
                            }
                        };

                        this._context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        this._context.Response.ContentType = MediaTypeNames.Application.Json;

                        await JsonSerializer.SerializeAsync(
                            this._context.Response.Body,
                            response, ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                            _context.RequestAborted
                        );

                        return false;
                    }
                    else {
                        return true;
                    }
                }
            }
        }

        public IResult GetTableClassStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo, [CallerMemberName] string callerMemberName = null) where T : JsonSerDe, new() {
            return Results.Ok(new ResponseJsonSingle<CTableClassModel>() {
                info = $"{StatusCodes.Status200OK} - {callerMemberName}",
                result = new CTableClassModel() {
                    table_name = typeof(T).Name,
                    properties = this._cs.GetTableClassStructureModel(jsonTypeInfo)
                }
            });
        }

        public IResult GetPocoStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo, [CallerMemberName] string callerMemberName = null) where T : JsonSerDe, new() {
            return Results.Ok(new ResponseJsonSingle<CPocoModel>() {
                info = $"{StatusCodes.Status200OK} - {callerMemberName}",
                result = new CPocoModel() {
                    poco_name = typeof(T).Name,
                    properties = this._cs.GetPocoStructureModel(jsonTypeInfo)
                }
            });
        }

    }

}
