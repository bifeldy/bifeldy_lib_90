using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Web;

namespace bifeldy_lib_90.Handlers {

    public interface IEndpointTarikDataHandler : IEndpointBaseHandler {
        Task<IResult> HitDimanaSaja<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> DirectDbDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> DirectDbDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> DirectDbDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> DirectDbDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitHo<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitHoFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitHoCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitHoDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitNonDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitNonDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitNonDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<IResult> HitNonDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
    }

    public sealed class CEndpointTarikDataHandler : CEndpointBaseHandler, IEndpointTarikDataHandler {

        public CEndpointTarikDataHandler(
            CronScheduler scheduler,
            //
            ILogger<CEndpointTarikDataHandler> logger,
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

        /* ** *** ** */

        public async Task<IResult> HitDimanaSaja<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                // fd :: Boleh NULL

                _ = await customService.ExecuteSebelumTarik(this._context, db, fd, searchQuery, sort, order, page, row);

                if (!forceFullDataSkipPaging) {
                    (decimal pages, decimal count, IEnumerable<TOutputJson> ls) = await customService.TarikDataPaging(jsonTypeInfoOutput, this._context, db, fd, searchQuery, sort, order, page, row);

                    _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order, page, row);

                    return Results.Ok(new ResponseJsonMulti<TOutputJson>() {
                        info = $"200 - {callerMemberName}",
                        results = ls,
                        pages = pages,
                        count = count
                    });
                }

                IHttpResponseBodyFeature hrbf = this._context.Features.Get<IHttpResponseBodyFeature>();
                if (hrbf != null) {
                    hrbf.DisableBuffering();
                }

                this._context.Response.StatusCode = 200;
                this._context.Response.ContentType = "application/x-ndjson";

                await this._context.Response.StartAsync();

                using (var writer = new StreamWriter(this._context.Response.Body)) {
                    IAsyncEnumerable<TOutputJson> iae = customService.TarikDataFullStream(jsonTypeInfoOutput, this._context, db, fd, searchQuery, sort, order);

                    await foreach (TOutputJson item in iae) {
                        string json = JsonSerializer.Serialize(item, jsonTypeInfoOutput);

                        await writer.WriteLineAsync(json);
                        await writer.FlushAsync();

                        await this._context.Response.Body.FlushAsync();
                    }
                };

                _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order);

                return Results.Empty;
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

        private async Task<IResult> ExportCsvDimanaSaja<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            customService ??= (IServiceTarikDataHandler)this._baseService;
            if (string.IsNullOrEmpty(delimiter)) {
                delimiter = "|";
            }

            string fileName = $"{DateTime.Now.Ticks}_{callerMemberName}";

            if (!string.IsNullOrEmpty(prefixFileName)) {
                fileName = $"{prefixFileName}_{fileName}";
            }

            (IDictionary<string, string> jsonKeysTableCustomColumns, string sqlCustomQuery, DynamicParameters sqlParam) = await customService.GetCustomQueryParam(this._context, db, fd, searchQuery, sort, order);
            string sqlQuery = customService.GetFullQuery(sqlCustomQuery, jsonKeysTableCustomColumns);

            string ipOrigin = this._context.Items["address_ip"].ToString();
            if (!fileName.StartsWith(ipOrigin)) {
                fileName = $"{ipOrigin}_{fileName}";
            }

            if (!fileName.ToLower().EndsWith(".csv")) {
                fileName += ".csv";
            }

            string filePath = Path.Combine(this._gs.CsvFolderPath, fileName);

            _ = await customService.ExecuteSebelumTarik(this._context, db, fd, searchQuery, sort, order);

            this._scheduler.EnqueueDynamicJob(
                $"ExportFile___{fileName}",
                async (IServiceProvider ___sp, CancellationToken ___ctx) => {
                    int MAX_CONN_SESS = 16;

                    IPostgres ___pg = ___sp.GetRequiredService<IPostgres>();
                    IGeneralRepository ___generalRepo = ___sp.GetRequiredService<IGeneralRepository>();
                    ILockerService ___locker = ___sp.GetRequiredService<ILockerService>();
                    CronScheduler ___sched = ___sp.GetRequiredService<CronScheduler>();

                    try {
                        _ = await ___locker.SemaphoreGlobalApp(callerMemberName, MAX_CONN_SESS, MAX_CONN_SESS).WaitAsync(-1);

                        if (___ctx.IsCancellationRequested) {
                            throw new Exception("Job Dibatalkan");
                        }

                        _ = await ___pg.BulkGetCsv(
                            sqlQuery, delimiter, fileName,
                            sqlParameter: sqlParam,
                            useDoubleQuote: false,
                            commandTimeoutSeconds: 0,
                            token: ___ctx
                        );

                        ___sched.EnqueueDynamicJob(
                            $"DeleteFile___{fileName}",
                            async (IServiceProvider ______sp, CancellationToken ______ctx) => {
                                if (File.Exists(filePath)) {
                                    File.Delete(filePath);
                                }

                                await Task.CompletedTask;
                            },
                            startedAt: DateTime.Now.AddHours(2)
                        );
                    }
                    catch {
                        if (File.Exists(filePath)) {
                            File.Delete(filePath);
                        }

                        throw;
                    }
                    finally {
                        _ = ___locker.SemaphoreGlobalApp(callerMemberName).Release();
                    }
                }
            );

            // _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order);

            return Results.Accepted(
                $"/downloader?completedOnly=true&fileType=csv&fileName={fileName}",
                new ResponseJsonSingle<string>() {
                    info = $"202 - {callerMemberName}",
                    result = fileName
                }
            );
        }

        private async Task<IResult> ExportDocsDimanaSaja<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;
                fileType ??= "HTML";

                if (!this._rs.FileType.ContainsKey(fileType.ToUpper())) {
                    throw new TidakMemenuhiException("Tipe Export Tidak Dapat Digunakan!");
                }

                (rdlcPath, dsName, fileType, IDictionary<string, string> reportParameters, IDictionary<string, string> jsonKeysTableCustomColumns, string sqlCustomQuery, DynamicParameters sqlParam) = await customService.GetCustomQueryParamExportDocs(rdlcPath, dsName, fileType, this._context, db, fd, searchQuery, sort, order);
                string sqlQuery = customService.GetFullQuery(sqlCustomQuery, jsonKeysTableCustomColumns);

                string ipOrigin = this._context.Items["address_ip"].ToString();

                string fileName = $"{DateTime.Now.Ticks}_{dsName}_{callerMemberName}";

                if (!fileName.StartsWith(ipOrigin)) {
                    fileName = $"{ipOrigin}_{fileName}";
                }

                if (!fileName.ToLower().EndsWith(fileType.ToLower())) {
                    fileName += $".{fileType.ToLower()}";
                }

                string filePath = Path.Combine(this._gs.TempFolderPath, fileName);

                _ = await customService.ExecuteSebelumTarik(this._context, db, fd, searchQuery, sort, order);

                this._scheduler.EnqueueDynamicJob(
                    $"ExportFile___{fileName}",
                    async (IServiceProvider ___sp, CancellationToken ___ctx) => {
                        int MAX_CONN_SESS = 16;

                        IPostgres ___pg = ___sp.GetRequiredService<IPostgres>();
                        IGeneralRepository ___generalRepo = ___sp.GetRequiredService<IGeneralRepository>();
                        ILockerService ___locker = ___sp.GetRequiredService<ILockerService>();
                        IRdlcService ___rdlc = ___sp.GetRequiredService<IRdlcService>();
                        CronScheduler ___sched = ___sp.GetRequiredService<CronScheduler>();

                        try {
                            _ = await ___locker.SemaphoreGlobalApp(callerMemberName, MAX_CONN_SESS, MAX_CONN_SESS).WaitAsync(-1);

                            if (___ctx.IsCancellationRequested) {
                                throw new Exception("Job Dibatalkan");
                            }

                            if (RuntimeFeature.IsDynamicCodeSupported) {
                                IEnumerable<TOutputJson> reportDataRowList = await ___pg.GetEnumerableAsync(
                                    jsonTypeInfoOutput, sqlQuery, sqlParam,
                                    token: ___ctx
                                );

                                RdlcReport report = ___rdlc.GeneratePdfWordExcelHtmlReport(
                                    rdlcPath,
                                    reportDataRowList,
                                    dsName,
                                    ___rdlc.CreateReportParameter(reportParameters),
                                    fileType
                                );

                                await File.WriteAllBytesAsync(filePath, report.Report, ___ctx);

                                ___sched.EnqueueDynamicJob(
                                    $"DeleteFile___{fileName}",
                                    async (IServiceProvider ______sp, CancellationToken ______ctx) => {
                                        if (File.Exists(filePath)) {
                                            File.Delete(filePath);
                                        }

                                        await Task.CompletedTask;
                                    },
                                    startedAt: DateTime.Now.AddHours(2)
                                );
                            }
                            else {
                                IAsyncEnumerable<TOutputJson> dataStream = ___pg.GetAsyncEnumerable(
                                    jsonTypeInfoOutput, sqlQuery, sqlParam,
                                    token: ___ctx
                                );

                                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096)) {
                                    await ___rdlc.GeneratePdfWordExcelHtmlReportExternal(
                                        ___ctx,
                                        fs,
                                        dataStream,
                                        jsonTypeInfoOutput,
                                        ___rdlc.CreateInfoWrapper(reportParameters),
                                        rdlcPath,
                                        dsName,
                                        fileType
                                    );
                                }
                            }
                        }
                        catch {
                            if (File.Exists(filePath)) {
                                File.Delete(filePath);
                            }

                            throw;
                        }
                        finally {
                            _ = ___locker.SemaphoreGlobalApp(callerMemberName).Release();
                        }
                    }
                );

                // _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order);

                return Results.Accepted(
                    $"/downloader?completedOnly=true&fileType={fileType}&fileName={fileName}",
                    new ResponseJsonSingle<string>() {
                        info = $"202 - {callerMemberName}",
                        result = fileName
                    }
                );
            }
            catch (TidakMemenuhiException tm) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = tm.Message
                    }
                });
            }
        }

        /* ** */

        private async Task<IResult> DefaultHandlerDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task<IResult>> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (isNonDc) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui HO / DC"
                        }
                    });
                }

                var f = (InputJsonDc)fd;

                IResult er = await this.CheckExcludeJenisDc(db, f.kode_dc, callerMemberName);
                if (er != null) {
                    return er;
                }

                bool isHo = await this._generalRepo.IsHo(db);
                if (!isHo) {
                    return await callback();
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

                    NameValueCollection queryDictionary = HttpUtility.ParseQueryString(u.Query);

                    f.secret = queryDictionary["secret"];
                    f.key = queryDictionary["key"];
                    f.token = queryDictionary["token"];

                    string jsonBody = this._cs.ObjectToJson((TInputJson)(object)f, jsonTypeInfoInput);
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))) {
                        this._context.Request.Body = ms;
                        return await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true);
                    }
                }
            }
            catch (TidakMemenuhiException tm) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = tm.Message
                    }
                });
            }
        }

        private async Task<IResult> HitDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.HitDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, false, customService, callerMemberName);
        }

        public async Task<IResult> HitDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, null, null, sort, order, true, customService, callerMemberName);
        }

        public async Task<IResult> HitDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportCsvDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportDocsDimanaSaja<TInputJson, TOutputJson>(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** ALTERNATIVE DC API ** */

        private async Task<IResult> DefaultHandlerDirectDbDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<InputJsonDc, IDatabase, IDatabase, Task<IResult>> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

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

                var f = (InputJsonDc)fd;

                IResult er = await this.CheckExcludeJenisDc(db, f.kode_dc, callerMemberName);
                if (er != null) {
                    return er;
                }

                await this._generalRepo.CheckKoordinatorHO(db, f.kode_dc);

                (IDatabase dbOraPg, IDatabase dbMsSql) = await this._generalRepo.OpenConnectionToDcFromHo(db, f.kode_dc, this._sp);
                if (dbOraPg == null) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName} (Mirror)",
                        result = new ResponseJsonMessage() {
                            message = $"Kode DC {f.kode_dc} tidak tersedia!"
                        }
                    });
                }

                return await callback(f, dbOraPg, dbMsSql);
            }
            catch (TidakMemenuhiException tm) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName} (Mirror)",
                    result = new ResponseJsonMessage() {
                        message = tm.Message
                    }
                });
            }
        }

        private async Task<IResult> DirectDbDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDirectDbDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return await this.HitDimanaSaja<TInputJson, TOutputJson>(dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> DirectDbDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DirectDbDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, false, customService, callerMemberName);
        }

        public async Task<IResult> DirectDbDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DirectDbDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, null, null, sort, order, true, customService, callerMemberName);
        }

        public async Task<IResult> DirectDbDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDirectDbDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return await this.ExportCsvDimanaSaja<TInputJson, TOutputJson>(dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> DirectDbDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerDirectDbDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return await this.ExportDocsDimanaSaja<TInputJson, TOutputJson>(rdlcPath, dsName, fileType, dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** *** ** */

        private async Task<IResult> DefaultHandlerHo<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task<IResult>> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isHo = await this._generalRepo.IsHo(db);
                if (!isHo) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui HO"
                        }
                    });
                }

                return await callback();
            }
            catch (TidakMemenuhiException tm) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = tm.Message
                    }
                });
            }
        }

        private async Task<IResult> HitHo<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerHo<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.HitDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitHo<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitHo<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, false, customService, callerMemberName);
        }

        public async Task<IResult> HitHoFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitHo<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, null, null, sort, order, true, customService, callerMemberName);
        }

        public async Task<IResult> HitHoCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerHo<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportCsvDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitHoDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerHo<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportDocsDimanaSaja<TInputJson, TOutputJson>(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** *** ** */

        private async Task<IResult> DefaultHandlerNonDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task<IResult>> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (!isNonDc) {
                    return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {callerMemberName}",
                        result = new ResponseJsonMessage() {
                            message = "Endpoint ini hanya dapat diakses melalui NON DC"
                        }
                    });
                }

                return await callback();
            }
            catch (TidakMemenuhiException tm) {
                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {callerMemberName}",
                    result = new ResponseJsonMessage() {
                        message = tm.Message
                    }
                });
            }
        }

        private async Task<IResult> HitNonDc<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging = false, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerNonDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.HitDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitNonDcPaging<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitNonDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, false, customService, callerMemberName);
        }

        public async Task<IResult> HitNonDcFull<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.HitNonDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, null, null, sort, order, true, customService, callerMemberName);
        }

        public async Task<IResult> HitNonDcCsv<TInputJson, TOutputJson>(IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerNonDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportCsvDimanaSaja<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, callerMemberName);
            }, customService, callerMemberName);
        }

        public async Task<IResult> HitNonDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, InputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return await this.DefaultHandlerNonDc<TInputJson, TOutputJson>(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, async () => {
                return await this.ExportDocsDimanaSaja<TInputJson, TOutputJson>(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, callerMemberName);
            }, customService, callerMemberName);
        }

    }

}
