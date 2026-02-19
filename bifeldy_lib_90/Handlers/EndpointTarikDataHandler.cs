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
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Web;

namespace bifeldy_lib_90.Handlers {

    public interface IEndpointTarikDataHandler : IEndpointBaseHandler {
        Task CreateResponseMessage(int statusCode, string responseJsonMessage, string suffixInfo = null, [CallerMemberName] string callerMemberName = null);
        Task HitDimanaSaja<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, string suffixInfo = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task DirectDbDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task DirectDbDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task DirectDbDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitHo<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitHoCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitHoDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitNonDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitNonDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task HitNonDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
    }

    public class CEndpointTarikDataHandler : CEndpointBaseHandler, IEndpointTarikDataHandler {

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

        public Task CreateResponseMessage(int statusCode, string responseJsonMessage, string suffixInfo = null, [CallerMemberName] string callerMemberName = null) {
            var response = new ResponseJsonSingle<ResponseJsonMessage>() {
                info = $"{statusCode} - {callerMemberName}",
                result = new ResponseJsonMessage() {
                    message = responseJsonMessage
                }
            };

            if (!string.IsNullOrEmpty(suffixInfo)) {
                response.info = $"{statusCode} - {callerMemberName} {suffixInfo}";
            }

            this._context.Response.StatusCode = statusCode;
            this._context.Response.ContentType = MediaTypeNames.Application.Json;

            return JsonSerializer.SerializeAsync(
                this._context.Response.Body,
                response, ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                this._context.RequestAborted
            );
        }

        /* ** *** ** */

        public async Task HitDimanaSaja<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, string suffixInfo = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                // fd :: Boleh NULL

                _ = await customService.ExecuteSebelumTarik(this._context, db, fd, searchQuery, sort, order, page, row);

                if (!forceFullDataSkipPaging) {
                    (decimal pages, decimal count, IAsyncEnumerable<TOutputJson> ls) = await customService.TarikDataPaging(jsonTypeInfoOutput, this._context, db, fd, searchQuery, sort, order, page, row);

                    this._context.Response.StatusCode = StatusCodes.Status200OK;
                    this._context.Response.ContentType = MediaTypeNames.Application.Json;

                    await this._context.Response.StartAsync();

                    await using (var writer = new Utf8JsonWriter(this._context.Response.BodyWriter)) {
                        writer.WriteStartObject();

                        string info = $"{StatusCodes.Status200OK} - {callerMemberName}";
                        if (!string.IsNullOrEmpty(suffixInfo)) {
                            info = $"{StatusCodes.Status200OK} - {callerMemberName} {suffixInfo}";
                        }

                        writer.WriteString("info", info);
                        writer.WritePropertyName("results");
                        writer.WriteStartArray();

                        await writer.FlushAsync(this._context.RequestAborted);

                        await foreach (TOutputJson item in ls) {
                            JsonSerializer.Serialize(writer, item, jsonTypeInfoOutput);
                        }

                        writer.WriteEndArray();
                        writer.WriteNumber("pages", pages);
                        writer.WriteNumber("count", count);
                        writer.WriteEndObject();

                        await writer.FlushAsync(this._context.RequestAborted);
                    }

                    _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order, page, row);
                }
                else {
                    IHttpResponseBodyFeature hrbf = this._context.Features.Get<IHttpResponseBodyFeature>();
                    hrbf?.DisableBuffering();

                    this._context.Response.StatusCode = StatusCodes.Status206PartialContent;
                    this._context.Response.ContentType = "application/x-ndjson";

                    this._context.Response.Headers.Append("X-Accel-Buffering", "no");

                    if (this._context.Response.Headers.ContainsKey("Content-Length")) {
                        _ = this._context.Response.Headers.Remove("Content-Length");
                    }

                    await this._context.Response.StartAsync();

                    IAsyncEnumerable<TOutputJson> iae = customService.TarikDataFullStream(jsonTypeInfoOutput, this._context, db, fd, searchQuery, sort, order);
                    await foreach (TOutputJson item in iae.WithCancellation(this._context.RequestAborted)) {
                        string json = JsonSerializer.Serialize(item, jsonTypeInfoOutput);
                        await this._context.Response.WriteAsync(json, this._context.RequestAborted);
                        await this._context.Response.Body.WriteAsync("\n"u8.ToArray(), this._context.RequestAborted);
                        await this._context.Response.Body.FlushAsync(this._context.RequestAborted);
                    }

                    _ = await customService.ExecuteSesudahTarik(this._context, db, fd, searchQuery, sort, order);
                }
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, suffixInfo, callerMemberName);
            }
        }

        private async Task ExportCsvDimanaSaja<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, string suffixInfo = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
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
                            _ = await ___locker.SemaphoreGlobalApp(callerMemberName, MAX_CONN_SESS, MAX_CONN_SESS).WaitAsync(-1, ___ctx);

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

                var response = new ResponseJsonSingle<string>() {
                    info = $"{StatusCodes.Status202Accepted} - {callerMemberName}",
                    result = fileName
                };

                this._context.Response.Headers.Location = $"/downloader?completedOnly=true&fileType=csv&fileName={fileName}";
                this._context.Response.StatusCode = StatusCodes.Status202Accepted;
                this._context.Response.ContentType = MediaTypeNames.Application.Json;

                await JsonSerializer.SerializeAsync(
                    this._context.Response.Body,
                    response, ResponseJsonSerializerContext.Default.ResponseJsonSingleString,
                    this._context.RequestAborted
                );
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, suffixInfo, callerMemberName);
            }
        }

        private async Task ExportDocsDimanaSaja<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, string suffixInfo = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
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
                            _ = await ___locker.SemaphoreGlobalApp(callerMemberName, MAX_CONN_SESS, MAX_CONN_SESS).WaitAsync(-1, ___ctx);

                            if (___ctx.IsCancellationRequested) {
                                throw new Exception("Job Dibatalkan");
                            }

                            if (RuntimeFeature.IsDynamicCodeSupported) {
                                IEnumerable<TOutputJson> reportDataRowList = await ___pg.GetListAsync(
                                    jsonTypeInfoOutput, sqlQuery, sqlParam,
                                    token: ___ctx
                                );

                                RdlcReport report = await ___rdlc.GeneratePdfWordExcelHtmlReport(
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

                                await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096)) {
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

                var response = new ResponseJsonSingle<string>() {
                    info = $"{StatusCodes.Status202Accepted} - {callerMemberName}",
                    result = fileName
                };

                this._context.Response.Headers.Location = $"/downloader?completedOnly=true&fileType={fileType}&fileName={fileName}";
                this._context.Response.StatusCode = StatusCodes.Status202Accepted;
                this._context.Response.ContentType = MediaTypeNames.Application.Json;

                await JsonSerializer.SerializeAsync(
                    this._context.Response.Body,
                    response, ResponseJsonSerializerContext.Default.ResponseJsonSingleString,
                    this._context.RequestAborted
                );
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, suffixInfo, callerMemberName);
            }
        }

        /* ** */

        private async Task DefaultHandlerDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (isNonDc) {
                    await this.CreateResponseMessage(StatusCodes.Status403Forbidden, "Endpoint ini hanya dapat diakses melalui HO / DC", null, callerMemberName);
                }
                else {
                    if (fd is not InputJsonDc f) {
                        throw new TidakMemenuhiException("Format Data Tidak Sesuai");
                    }

                    bool success = await this.CheckExcludeJenisDcNoResult(db, f.kode_dc, callerMemberName);
                    if (success) {
                        bool isHo = await this._generalRepo.IsHo(db);
                        if (!isHo) {
                            await callback();
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
                                await this.CreateResponseMessage(StatusCodes.Status403Forbidden, e, null, callerMemberName);
                            }
                            else {
                                NameValueCollection queryDictionary = HttpUtility.ParseQueryString(u.Query);

                                f.secret = queryDictionary["secret"];
                                f.key = queryDictionary["key"];
                                f.token = queryDictionary["token"];

                                string jsonBody = this._cs.ObjectToJson((TInputJson)(object)f, jsonTypeInfoInput);
                                await using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))) {
                                    this._context.Request.Body = ms;
                                    _ = await this._http.ForwardRequest(u.ToString(), this._context.Request, this._context.Response, true, cancellationToken: this._context.RequestAborted);
                                }
                            }
                        }
                    }
                }
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, null, callerMemberName);
            }
        }

        public Task HitDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.HitDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportCsvDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportDocsDimanaSaja(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** ALTERNATIVE DC API ** */

        private async Task DefaultHandlerDirectDbDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<InputJsonDc, IDatabase, IDatabase, Task> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                bool isHo = await this._generalRepo.IsHo(db);

                if (isNonDc || !isHo) {
                    await this.CreateResponseMessage(StatusCodes.Status403Forbidden, "Endpoint ini hanya dapat diakses melalui HO", "(Mirror)", callerMemberName);
                }
                else {
                    if (fd is not InputJsonDc f) {
                        throw new TidakMemenuhiException("Format Data Tidak Sesuai");
                    }

                    bool success = await this.CheckExcludeJenisDcNoResult(db, f.kode_dc, callerMemberName);
                    if (success) {
                        await this._generalRepo.CheckKoordinatorHO(db, f.kode_dc);

                        (IDatabase dbOraPg, IDatabase dbMsSql) = await this._generalRepo.OpenConnectionToDcFromHo(db, f.kode_dc, this._sp);
                        if (dbOraPg == null) {
                            await this.CreateResponseMessage(StatusCodes.Status404NotFound, $"Kode DC {f.kode_dc} tidak tersedia!", null, callerMemberName);
                        }
                        else {
                            await callback(f, dbOraPg, dbMsSql);
                        }
                    }

                }
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, "(Mirror)", callerMemberName);
            }
        }

        public Task DirectDbDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDirectDbDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return this.HitDimanaSaja(dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, "(Mirror)", callerMemberName);
            }, customService, callerMemberName);
        }

        public Task DirectDbDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDirectDbDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return this.ExportCsvDimanaSaja(dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, "(Mirror)", callerMemberName);
            }, customService, callerMemberName);
        }

        public Task DirectDbDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerDirectDbDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, (InputJsonDc f, IDatabase dbOraPg, IDatabase dbMsSql) => {
                return this.ExportDocsDimanaSaja(rdlcPath, dsName, fileType, dbOraPg, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, "(Mirror)", callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** *** ** */

        private async Task DefaultHandlerHo<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isHo = await this._generalRepo.IsHo(db);
                if (!isHo) {
                    await this.CreateResponseMessage(StatusCodes.Status403Forbidden, "Endpoint ini hanya dapat diakses melalui HO", null, callerMemberName);
                }
                else {
                    await callback();
                }
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, null, callerMemberName);
            }
        }

        public Task HitHo<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerHo(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.HitDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitHoCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerHo(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportCsvDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitHoDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerHo(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportDocsDimanaSaja(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        /* ** *** ** */

        private async Task DefaultHandlerNonDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, Func<Task> callback, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            try {
                customService ??= (IServiceTarikDataHandler)this._baseService;

                bool isNonDc = await this._generalRepo.IsNonDc(db);
                if (!isNonDc) {
                    await this.CreateResponseMessage(StatusCodes.Status403Forbidden, "Endpoint ini hanya dapat diakses melalui NON DC", null, callerMemberName);
                }
                else {
                    await callback();
                }
            }
            catch (TidakMemenuhiException tm) {
                await this.CreateResponseMessage(StatusCodes.Status400BadRequest, tm.Message, null, callerMemberName);
            }
        }

        public Task HitNonDc<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string page, string row, string sort, string order, bool forceFullDataSkipPaging, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerNonDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.HitDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, page, row, sort, order, forceFullDataSkipPaging, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitNonDcCsv<TInputJson, TOutputJson>(IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, string prefixFileName = null, string delimiter = null, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerNonDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportCsvDimanaSaja(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, prefixFileName, delimiter, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

        public Task HitNonDcDocs<TInputJson, TOutputJson>(string rdlcPath, string dsName, string fileType, IDatabase db, TInputJson fd, JsonTypeInfo<TInputJson> jsonTypeInfoInput, JsonTypeInfo<TOutputJson> jsonTypeInfoOutput, string searchQuery, string sort, string order, IServiceTarikDataHandler customService = null, [CallerMemberName] string callerMemberName = null) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            return this.DefaultHandlerNonDc(db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, () => {
                return this.ExportDocsDimanaSaja(rdlcPath, dsName, fileType, db, fd, jsonTypeInfoInput, jsonTypeInfoOutput, searchQuery, sort, order, customService, null, callerMemberName);
            }, customService, callerMemberName);
        }

    }

}