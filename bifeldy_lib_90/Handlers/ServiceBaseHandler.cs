using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Handlers {

    public interface IServiceBaseHandler { }

    public abstract class CServiceBaseHandler : IServiceBaseHandler {

        protected readonly ILogger<CServiceBaseHandler> _logger;
        protected readonly IServiceProvider _sp;
        protected readonly IConverterService _cs;
        protected readonly IGeneralRepository _generalRepo;

        protected readonly ulong MAX_ROW_PER_PAGE = 500;
        protected readonly ulong MAX_CHUNK_SIZE = 2048;

        private string logTfApiName = null;
        protected string LogTfApiName {
            get => !string.IsNullOrEmpty(this.logTfApiName) ? this.logTfApiName : this.GetType().Name;
            set => this.logTfApiName = value;
        }

        public CServiceBaseHandler(
            ILogger<CServiceBaseHandler> logger,
            IServiceProvider sp,
            IConverterService cs,
            IGeneralRepository generalRepo
        ) {
            this._logger = logger;
            this._sp = sp;
            this._cs = cs;
            this._generalRepo = generalRepo;
        }

        protected async Task CatatLogTfApi<TFormDataStream>(HttpContext http, IDatabase db, object formDataStream, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, string statusErrMsg = null, DateTime? dateTimeStart = null) where TFormDataStream : JsonSerDe, new() {
            if (dateTimeStart == null) {
                dateTimeStart = (DateTime)http.Request.HttpContext.Items["request_start_at"];
            }

            string typeFile = $"{this.logTfApiName} - {http.Request.Method}";
            if (typeFile.Length > 45) {
                typeFile = typeFile[..45];
            }

            string ipOrigin = http.Items["address_ip_proxy"].ToString();

            string data_json = null;
            if (formDataStream != null) {
                Type type = formDataStream.GetType();

                if (typeof(Stream).IsAssignableFrom(type)) {
                    data_json = "[_01_DATA_STREAM_10_]";
                }
                else {
                    try {
                        data_json = this._cs.ObjectToJson((TFormDataStream)formDataStream, jsonTypeInfo);
                    }
                    catch {
                        data_json = "[_|_UNKNOWN_|_]";
                    }
                }
            }

            string hostPort = http.Request.Host.Host;
            int? port = http.Request.Host.Port;
            if (port != null) {
                hostPort += $":{port}";
            }

            string requestPath = http.Request.Path.Value;
            if (http.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                string proxyPath = pathBase.Last();
                if (!string.IsNullOrEmpty(proxyPath)) {
                    if (!requestPath.StartsWith(proxyPath)) {
                        requestPath = $"{proxyPath}{requestPath}";
                    }
                }
            }

            string url_destination = $"http://{hostPort}{requestPath}";

            // Max 100
            if (url_destination.Length > 95) {
                url_destination = url_destination[..95];
                url_destination += "...";
            }

            // Max 500
            string token = http.Items["token"].ToString();
            if (token.Length > 495) {
                token = token[..495];
                token += "...";
            }

            // Max 1000
            if (statusErrMsg?.Length > 995) {
                statusErrMsg = statusErrMsg[..995];
                statusErrMsg += "...";
            }

            string sqlQuery = $@"
                INSERT INTO dc_logtf_api_t (kode_dc, tgl_request, ip_client, type_file, status, start_process, end_process, data_json, url_destination, token, keterangan)
                VALUES (:kode_dc, :tgl_request, :ip_client, :type_file, :status, :start_process, :end_process, :data_json, :url_destination, :token, :keterangan)
            ";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("kode_dc", await this._generalRepo.GetKodeDc(db));
            sqlParam.Add("tgl_request", dateTimeStart?.Date);
            sqlParam.Add("ip_client", ipOrigin);
            sqlParam.Add("type_file", typeFile);
            sqlParam.Add("status", string.IsNullOrEmpty(statusErrMsg) ? "SUKSES" : "ERROR");
            sqlParam.Add("start_process", dateTimeStart);
            sqlParam.Add("end_process", DateTime.Now);
            sqlParam.Add("data_json", data_json);
            sqlParam.Add("url_destination", url_destination);
            sqlParam.Add("token", token);
            sqlParam.Add("keterangan", statusErrMsg);

            _ = await db.ExecQueryAsync(sqlQuery, sqlParam, token: http.RequestAborted);
        }

        protected static async IAsyncEnumerable<T> GetRequestJsonStreamData<T>(HttpContext http, JsonTypeInfo<T> jsonTypeInfo) where T : JsonSerDe, new() {
            string contentType = http.Request.ContentType;

            if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase)) {
                IAsyncEnumerable<T> iae = JsonSerializer.DeserializeAsyncEnumerable(http.Request.Body, jsonTypeInfo, http.RequestAborted);
                await foreach (T item in iae.WithCancellation(http.RequestAborted)) {
                    if (item == null) {
                        continue;
                    }

                    yield return item;
                }
            }
            else if (string.Equals(contentType, "application/x-ndjson", StringComparison.OrdinalIgnoreCase)) {
                using (var reader = new StreamReader(http.Request.Body)) {
                    string line = null;

                    while ((line = await reader.ReadLineAsync(http.RequestAborted)) != null) {
                        T item = default;

                        try {
                            item = JsonSerializer.Deserialize(line, jsonTypeInfo);
                        }
                        catch {
                            throw new Exception("Format X-(ND)JSON Harus Per Baris 1 Object Lengkap");
                        }

                        if (item == null) {
                            continue;
                        }

                        yield return item;
                    }
                }
            }
            else {
                throw new Exception($"Streaming Untuk Content-Type '{contentType}' Tidak Tersedia");
            }
        }

        protected async Task<int> InsertDataFromTempWithUpdrecIdCurrSession<T>(JsonTypeInfo<T> jsonTypeInfo, HttpContext http, IDatabase db, List<T> ls, string currSession, bool mergeOnly = true) {
            string tableName = typeof(T).Name.ToUpper();
            string tempTableName = $"{tableName}_TEMP";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("updrec_id", currSession);

            _ = await db.ExecQueryAsync(
                $@"
                    DELETE FROM {tempTableName}
                    WHERE updrec_id = :updrec_id
                ",
                sqlParam,
                token: http.RequestAborted
            );

            int totalDataInserted = 0;
            foreach (T[] data in ls.Chunk((int)this.MAX_CHUNK_SIZE)) {
                totalDataInserted += await db.BulkInsertInto(tempTableName, data);
            }

            IList<JsonPropertyInfo> properties = jsonTypeInfo.Properties;
            string[] columnNames = [.. properties.Select(d => d.Name.ToUpper())];

            IEnumerable<string> pkNames = properties.Where(d => {
                return d.AttributeProvider
                    .GetCustomAttributes(true)
                    .Where(a => typeof(KeyAttribute).IsAssignableFrom(a.GetType()))
                    .Any();
            }).Select(d => d.Name.ToUpper());

            tableName = tableName.ToLower();
            tempTableName = tempTableName.ToLower();

            var colUpdate = new List<string>();
            foreach (string column in columnNames) {
                if (!pkNames.Contains(column)) {
                    colUpdate.Add(column);
                }
            }

            string columnName = string.Join(", ", columnNames).ToLower();

            try {
                _ = await db.TransactionStartAndOpenAsync();

                if (!mergeOnly) {
                    sqlParam = new DynamicParameters();
                    sqlParam.Add("updrec_id", currSession);

                    _ = await db.ExecQueryAsync($@"DELETE FROM {tableName}", sqlParam);
                }

                string sqlQuery = $@"
                    INSERT INTO {tableName} ({columnName})
                        SELECT {columnName}
                        FROM {tempTableName}
                        WHERE updrec_id = :updrec_id
                ";

                if (mergeOnly) {
                    string pkName = string.Join(", ", pkNames).ToLower();
                    string colUpdateEx = string.Join(", ", colUpdate.Select(c => $"{c} = excluded.{c}")).ToLower();

                    sqlQuery += $@"
                        ON CONFLICT ({pkName})
                        DO
                            UPDATE SET
                                {colUpdateEx}
                    ";
                }

                sqlParam = new DynamicParameters();
                sqlParam.Add("updrec_id", currSession);

                int res = await db.ExecQueryWithResultAsync(sqlQuery, sqlParam, token: http.RequestAborted);

                await db.TransactionCommitAndCloseAsync();

                return res;
            }
            catch {
                await db.TransactionRollbackAndCloseAsync();
                throw;
            }
        }

    }

}