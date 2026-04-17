using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.Extensions.Options;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Web;

namespace bifeldy_lib_90.Repositories {

    public interface IApiDcListRepository {
        Task<bool> Create(IDatabase db, ListApiDc apiDcList);
        Task<(List<ListApiDc>, decimal, decimal)> GetAll(IDatabase db);
        Task<bool> Update(IDatabase db, ListApiDc apiDcList);
        Task<decimal?> Ping(IDatabase db, ListApiDc apiDcList);
    }

    public sealed class CApiDcListRepository : CRepository, IApiDcListRepository {

        private readonly EnvVar _env;
        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IChiperService _chiper;
        private readonly IConverterService _cs;
        private readonly IGeneralRepository _generalRepo;

        public CApiDcListRepository(
            IOptions<EnvVar> env,
            IApplicationService @as,
            IHttpService http,
            IChiperService chiper,
            IConverterService cs,
            IGeneralRepository generalRepo
        ) {
            this._env = env.Value;
            this._as = @as;
            this._http = http;
            this._chiper = chiper;
            this._cs = cs;
            this._generalRepo = generalRepo;
        }

        public async Task<bool> Create(IDatabase db, ListApiDc apiDcList) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("dc_kode", apiDcList.DC_KODE.ToUpper());
            sqlParam.Add("app_name", apiDcList.APP_NAME);
            sqlParam.Add("api_host", apiDcList.API_HOST);
            sqlParam.Add("api_path", apiDcList.API_PATH);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO api_dc_t (dc_kode, app_name, api_host, api_path)
                    VALUES (:dc_kode, :app_name, :api_host, :api_path)
                ",
                sqlParam
            );

            return res > 0;
        }

        public async Task<(List<ListApiDc>, decimal, decimal)> GetAll(IDatabase db) {
            string sqlQuery = @"
                SELECT
                    a.dc_kode, a.flag_dbpg,
                    COALESCE(a.ip_nginx_cloud, a.ip_nginx) AS ip_nginx,
                    a.user_nginx, a.pass_nginx,
                    b.app_name, b.api_host, b.api_path,
                    c.last_online, c.version, c.port_grpc,
                    COALESCE(b.api_path, '/datadc' || LOWER(a.dc_kode) || '/api/') default_api_path
                FROM
                    dc_tabel_ip_t a
                    LEFT JOIN api_dc_t b ON (
                        a.dc_kode = b.dc_kode
                        AND UPPER(b.app_name) = :app_name
                    )
                    LEFT JOIN (
                        SELECT d.dc_kode, d.ip_origin, d.version, d.port_grpc, MAX(last_online) AS last_online
                        FROM api_ping_t d
                        WHERE UPPER(app_name) = :app_name
                        GROUP BY d.dc_kode, d.ip_origin, d.version, d.port_grpc
                    ) c ON (
                        a.dc_kode = c.dc_kode
                        AND COALESCE(b.api_host, COALESCE(a.ip_nginx_cloud, a.ip_nginx)) = c.ip_origin
                    )
                ORDER BY
                    b.api_path, a.dc_kode
            ";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());

            List<ListApiDc> ls = await db.GetListAsync(
                ListApiDcJsonSerializerContext.Default.ListApiDc,
                sqlQuery,
                sqlParam
            );

            return (ls, ls.Count, 1);
        }

        public async Task<bool> Update(IDatabase db, ListApiDc apiDcList) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("dc_kode", apiDcList.DC_KODE.ToUpper());
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("api_host", apiDcList.API_HOST);
            sqlParam.Add("api_path", apiDcList.API_PATH);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    UPDATE api_dc_t
                    SET api_host = :api_host, api_path = :api_path
                    WHERE UPPER(dc_kode) = :dc_kode AND UPPER(app_name) = :app_name
                ",
                sqlParam
            );

            return res > 0;
        }

        /* ** */

        public async Task<decimal?> Ping(IDatabase db, ListApiDc apiDcList) {
            apiDcList.PING_PONG = null;

            if (!string.IsNullOrEmpty(apiDcList.API_PATH)) {
                string separator = "/api/";
                string hostApiDc = string.IsNullOrEmpty(apiDcList?.API_HOST) ? apiDcList?.IP_NGINX : apiDcList?.API_HOST;

                var uri = new Uri($"http://{hostApiDc}{apiDcList.API_PATH}");
                string[] urls = uri.ToString().Split(separator);
                if (urls.Length == 2) {
                    urls[1] = "ping-pong";

                    string url = string.Join(separator, urls);
                    uri = new Uri(url);

                    string hashed = this._chiper.HashText(this._as.AppName);

                    NameValueCollection queryUrlDc = HttpUtility.ParseQueryString(uri.Query);
                    queryUrlDc.Set("key", hashed);
                    queryUrlDc.Set("secret", hashed);

                    var uriBuilder = new UriBuilder(uri) {
                        Query = queryUrlDc.ToString()
                    };

                    uri = uriBuilder.Uri;
                    url = uri.ToString();

                    string kodeDc = await this._generalRepo.GetKodeDc(db);

                    long startTime = Stopwatch.GetTimestamp();
                    HttpResponseMessage res = await this._http.PutData(
                        url,
                        new InputJsonDcPingPong() {
                            kode_dc = kodeDc,
                            version = this._as.AppVersion,
                            port_api = this._env.API_PORT,
                            port_grpc = 0
                        },
                        InputJsonDcPingPongJsonSerializerContext.Default.InputJsonDcPingPong
                    );

                    if (!res.IsSuccessStatusCode) {
                        string errMsg = res.ReasonPhrase;

                        try {
                            string jsonString = await res.Content.ReadAsStringAsync();

                            ResponseJsonSingle<ResponseJsonMessage> r = this._cs.JsonToObject(
                                jsonString,
                                ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                            );

                            errMsg = r.result.message;
                        }
                        catch {
                            //
                        }

                        throw new TidakMemenuhiException($"Tidak Dapat Tersambung Ke {hostApiDc} :: {errMsg}");
                    }

                    long endTime = Stopwatch.GetTimestamp();
                    decimal elapsedMs = (decimal)(endTime - startTime) / (Stopwatch.Frequency / 1000);

                    apiDcList.PING_PONG = decimal.Round(elapsedMs, 1, MidpointRounding.AwayFromZero);
                }
            }

            return apiDcList.PING_PONG;
        }

    }

}