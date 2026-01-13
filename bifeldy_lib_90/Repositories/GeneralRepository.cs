using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Confluent.Kafka;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Specialized;
using System.Web;

namespace bifeldy_lib_90.Repositories {

    public interface IGeneralRepository : IRepository {
        Task<string> GetURLWebService(IDatabase db, string webType);
        Task<bool> SaveKafkaToTable(IDatabase db, string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName);
        Task<KAFKA_SERVER_T> GetKafkaServerInfo(IDatabase db, string topicName);
        Task<List<DC_TABEL_V>> GetListBranchDbInformation(IDatabase db, string kodeDcInduk);
        Task<IDictionary<string, IDatabase>> GetListBranchDbConnection(IDatabase db, string kodeDcInduk, IServiceProvider sp);
        Task<(IDatabase, IDatabase)> OpenConnectionToDcFromHo(IDatabase db, string kodeDcTarget, IServiceProvider sp);
        Task GetDcApiPathAppFromHo(IDatabase db, HttpRequest request, string dcKode, Action<string, Uri> Callback);
        Task<string> GetAppHoApiUrlBase(IDatabase db, string apiPath);
        Task CheckKoordinatorHO(IDatabase db, string kodeDc);
    }

    public class CGeneralRepository : CRepository, IGeneralRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IChiperService _chiper;
        private readonly IConverterService _converter;

        private IDictionary<
            string, IDictionary<string, IDatabase>
        > BranchConnectionInfo { get; } = new Dictionary<
            string, IDictionary<string, IDatabase>
        >(StringComparer.OrdinalIgnoreCase);

        public CGeneralRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IHttpService http,
            IChiperService chiper,
            IConverterService converter
        ) {
            this._envVar = envVar.Value;
            this._as = @as;
            this._http = http;
            this._chiper = chiper;
            this._converter = converter;
        }

        /** Custom Queries */

        public Task<string> GetURLWebService(IDatabase db, string webType) {
            string sqlQuery = "SELECT web_url FROM dc_webservice_t WHERE UPPER(web_type) = :web_type";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("web_type", webType.ToUpper());

            return db.ExecScalarAsync<string>(sqlQuery, sqlParam);
        }

        public Task<bool> SaveKafkaToTable(IDatabase db, string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName) {
            string sqlQuery = $@"
                INSERT INTO {logTableName} (TPC, OFFS, PARTT, KEY, VAL, TMSTAMP)
                VALUES (:tpc, :offs, :partt, :key, :value, :tmstmp)
            ";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("tpc", topic);
            sqlParam.Add("offs", offset);
            sqlParam.Add("partt", partition);
            sqlParam.Add("key", msg.Key);
            sqlParam.Add("value", msg.Value);
            sqlParam.Add("tmstmp", msg.Timestamp.UtcDateTime);

            return db.ExecQueryAsync(sqlQuery, sqlParam);
        }

        public Task<KAFKA_SERVER_T> GetKafkaServerInfo(IDatabase db, string topicName) {
            string sqlQuery = "SELECT * FROM kafka_server_t WHERE UPPER(topic) = :topic_name";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("topic_name", topicName.ToUpper());

            return db.ExecScalarAsync(
                KAFKA_SERVER_T_JsonSerializerContext.Default.KAFKA_SERVER_T,
                sqlQuery, sqlParam
            );
        }

        /* ** */

        public async Task<List<DC_TABEL_V>> GetListBranchDbInformation(IDatabase db, string kodeDcInduk) {
            string url = await this.GetURLWebService(db, "SYNCHO") ?? this._envVar.WS_SYNCHO;
            url += kodeDcInduk;

            HttpResponseMessage httpResponse = await this._http.PostData(url, null);

            string httpResString = await httpResponse.Content.ReadAsStringAsync();

            return this._converter.JsonToObject(httpResString, DC_TABEL_V_JsonSerializerContext.Default.ListDC_TABEL_V);
        }

        //
        // Akses Langsung Ke Database Cabang
        // Tembak Ambil Info Dari Service Mas Edwin :) HO
        // Atur URL Di `appsettings.json` -> ws_syncho
        //
        // Item1 => bool :: Apakah Menggunakan Postgre
        // Item2 => IDatabase :: Koneksi Ke Database Oracle / Postgre (Tidak Ada SqlServer)
        //
        // IDictionary<string, (bool, IDatabase)> dbPgBranch = await GetListBranchDbConnection(..., "G001", ...);
        // var res = dbPgBranch["G055"].Item2.ExecScalarAsync<...>(...);
        //
        public async Task<IDictionary<string, IDatabase>> GetListBranchDbConnection(IDatabase db, string kodeDcInduk, IServiceProvider sp) {
            if (!this.BranchConnectionInfo.ContainsKey(kodeDcInduk)) {
                IDictionary<string, IDatabase> dbCons = new Dictionary<string, IDatabase>(StringComparer.OrdinalIgnoreCase);

                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(db, kodeDcInduk);
                foreach (DC_TABEL_V dbi in dbInfo) {
                    IPostgres postgres = sp.GetRequiredService<IPostgres>();

                    IDatabase dbPgBranch = postgres.NewExternalConnection(
                        dbi.DBPG_IP ?? dbi.IP_DB,
                        dbi.DBPG_PORT ?? dbi.DB_PORT.ToString(),
                        dbi.DBPG_USER ?? dbi.DB_USER_NAME,
                        dbi.DBPG_PASS ?? dbi.DB_PASSWORD,
                        dbi.DBPG_NAME ?? dbi.DB_SID
                    );

                    dbCons.Add(dbi.TBL_DC_KODE.ToUpper(), dbPgBranch);
                }

                this.BranchConnectionInfo[kodeDcInduk] = dbCons;
            }

            return this.BranchConnectionInfo[kodeDcInduk];
        }

        public async Task<(IDatabase, IDatabase)> OpenConnectionToDcFromHo(IDatabase db, string kodeDcTarget, IServiceProvider sp) {
            IPostgres postgres = sp.GetRequiredService<IPostgres>();
            IMsSQL mssql = sp.GetRequiredService<IMsSQL>();

            IDatabase dbConHo = db;
            bool isHo = await this.IsHo(db);
            if (!isHo) {
                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(db, "DCHO");

                DC_TABEL_V dcho = dbInfo.FirstOrDefault();
                if (dcho != null) {
                    dbConHo = postgres.NewExternalConnection(
                        dcho.DBPG_IP ?? dcho.IP_DB,
                        dcho.DBPG_PORT ?? dcho.DB_PORT.ToString(),
                        dcho.DBPG_USER ?? dcho.DB_USER_NAME,
                        dcho.DBPG_PASS ?? dcho.DB_PASSWORD,
                        dcho.DBPG_NAME ?? dcho.DB_SID
                    );
                }
            }

            IDatabase dbPgDc = null;
            IDatabase dbSqlDc = null;

            if (dbConHo != null) {
                string sqlQuery = "SELECT * FROM dc_tabel_ip_t WHERE UPPER(dc_kode) = :dc_kode";

                var sqlParam = new DynamicParameters();
                sqlParam.Add("dc_kode", kodeDcTarget.ToUpper());

                DC_TABEL_IP_T dbi = await dbConHo.ExecScalarAsync(
                    DC_TABEL_IP_T_JsonSerializerContext.Default.DC_TABEL_IP_T,
                    sqlQuery, sqlParam
                );

                if (dbi != null) {
                    dbPgDc = postgres.NewExternalConnection(
                        dbi.DBPG_IP ?? dbi.IP_DB,
                        dbi.DBPG_PORT ?? dbi.DB_PORT.ToString(),
                        dbi.DBPG_USER ?? dbi.DB_USER_NAME,
                        dbi.DBPG_PASS ?? dbi.DB_PASSWORD,
                        dbi.DBPG_NAME ?? dbi.DB_SID
                    );

                    if (
                        !string.IsNullOrEmpty(dbi.DB_IP_SQL) &&
                        !string.IsNullOrEmpty(dbi.DB_USER_SQL) &&
                        !string.IsNullOrEmpty(dbi.DB_PWD_SQL) &&
                        !string.IsNullOrEmpty(dbi.SCHEMA_DPD)
                    ) {
                        dbSqlDc = mssql.NewExternalConnection(dbi.DB_IP_SQL, dbi.DB_USER_SQL, dbi.DB_PWD_SQL, dbi.SCHEMA_DPD);
                    }
                }
            }

            return (dbPgDc, dbSqlDc);
        }

        public async Task GetDcApiPathAppFromHo(IDatabase db, HttpRequest request, string dcKode, Action<string, Uri> callback) {
            bool isHo = await this.IsHo(db);
            if (!isHo) {
                throw new TidakMemenuhiException("Khusus HO");
            }

            string sqlQuery = @"
                SELECT
                    a.dc_kode,
                    a.ip_nginx,
                    b.api_host,
                    b.api_path
                FROM
                    dc_tabel_ip_t a
                    LEFT JOIN api_dc_t b ON (
                        a.dc_kode = b.dc_kode
                        AND UPPER(b.app_name) = :app_name
                    )
                WHERE
                    UPPER(a.dc_kode) = :kode_dc
            ";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("kode_dc", dcKode.ToUpper());

            ListApiDc dbi = await db.ExecScalarAsync(
                ListApiDcJsonSerializerContext.Default.ListApiDc,
                sqlQuery, sqlParam
            );

            string hostApiDc = string.IsNullOrEmpty(dbi?.API_HOST) ? dbi?.IP_NGINX : dbi?.API_HOST;
            if (dbi == null || string.IsNullOrEmpty(hostApiDc)) {
                callback($"Kode DC {dcKode.ToUpper()} tidak tersedia!", null);
            }
            else {
                string separator = $"/{Bifeldy.API_PREFIX}/";

                //
                // dotnet blablabla.dll
                //
                // http://127.x.xx.xxx/blablablaHOSIM/api/bliblibli
                // http://127.x.xx.xxx/blablablaHO/api/bliblibli
                // /blablablaHOSIM/api/bliblibli
                // /blablablaHO/api/bliblibli
                //
                // http://127.x.xx.xxx/blablablaGXXXSIM/api/bliblibli
                // http://127.x.xx.xxx/blablablaGXXX/api/bliblibli
                // /blablablaGXXXSIM/api/bliblibli
                // /blablablaGXXX/api/bliblibli
                //
                string currentPath = request.Path.Value;
                if (!string.IsNullOrEmpty(currentPath)) {
                    string findUrl = $"{this._as.AppName.ToUpper()}HO";
                    if (currentPath.ToUpper().Contains($"/{findUrl}")) {
                        int idx = currentPath.ToUpper().IndexOf(findUrl);
                        if (idx >= 0) {
                            idx += this._as.AppName.Length;
                            currentPath = $"{currentPath[..idx]}{dcKode.ToUpper()}{currentPath[(idx + 2)..]}";
                        }
                    }
                }

                string pathApiDc = string.IsNullOrEmpty(dbi.API_PATH) ? currentPath : $"{dbi.API_PATH}{currentPath?.Split(separator).Last()}";
                var urlApiDc = new Uri($"http://{hostApiDc}{pathApiDc}{request.QueryString.Value}");

                // API Khusus Bypass ~ Case Sensitive
                NameValueCollection queryApiDc = HttpUtility.ParseQueryString(urlApiDc.Query);
                string hashText = this._chiper.HashText(this._as.AppName);

                request.Headers["x-secret"] = hashText;
                queryApiDc.Set("secret", hashText);

                request.Headers["x-api-key"] = hashText;
                queryApiDc.Set("key", hashText);

                if (request.HttpContext.Items["token"] != null) {
                    string token = request.HttpContext.Items["token"].ToString();
                    request.Headers.Authorization = token;
                    request.Headers["x-access-token"] = token;
                    queryApiDc.Set("token", token);
                }

                if (request.HttpContext.Items["address_ip"] != null) {
                    string addrIp = request.HttpContext.Items["address_ip"].ToString();
                    queryApiDc.Set("mask_ip", await this._chiper.EncryptText(addrIp));
                }

                var uriBuilder = new UriBuilder(urlApiDc) {
                    Query = queryApiDc.ToString()
                };

                callback(null, uriBuilder.Uri);
            }
        }

        public async Task<string> GetAppHoApiUrlBase(IDatabase db, string apiPath) {
            //
            // http://xxx.xxx.xxx.xxx/{appNameAsPath}/api?secret=*********
            //
            string appNameAsPath = this._as.AppName.ToUpper();
            string apiUrl = await db.ExecScalarAsync<string>($@"
                SELECT web_url
                FROM dc_webservice_t
                WHERE web_type = '{appNameAsPath}_API_URL_BASE'
            ");

            if (string.IsNullOrEmpty(apiUrl)) {
                throw new Exception($"API URL Web Service '{appNameAsPath}_API_URL_BASE' Tidak Tersedia");
            }

            var baseUri = new Uri(apiUrl);
            NameValueCollection baseQuery = HttpUtility.ParseQueryString(baseUri.Query);

            string url = $"{baseUri.Scheme}://";
            if (!string.IsNullOrEmpty(baseUri.UserInfo)) {
                url += $"{baseUri.UserInfo}@";
            }

            url += $"{baseUri.Host}:{baseUri.Port}";

            var apiUri = new Uri(apiPath);
            NameValueCollection apiQuery = HttpUtility.ParseQueryString(apiUri.Query);

            foreach (string aq in baseQuery.AllKeys) {
                apiQuery.Set(aq, baseQuery.Get(aq));
            }

            var uriBuilder = new UriBuilder(url) {
                Path = $"{baseUri.AbsolutePath}{apiUri.AbsolutePath}",
                Query = apiQuery.ToString()
            };
            return uriBuilder.ToString();
        }

        public async Task CheckKoordinatorHO(IDatabase db, string kodeDc) {
            if (string.IsNullOrEmpty(kodeDc)) {
                throw new TidakMemenuhiException("Data Tidak Lengkap");
            }

            string targetJenisDc = await this.GetJenisDc(db, kodeDc.ToUpper());

            if (Enum.TryParse(targetJenisDc.ToUpper(), true, out EJenisDc _eJenisDc)) {
                bool isDcHo = await this.IsDcHo(db);
                bool isWhHo = await this.IsWhHo(db);

                string exception = null;
                if (isDcHo && _eJenisDc == EJenisDc.IPLAZA) {
                    exception = "Silahkan Gunakan WH HO Untuk Akses Ke DC IPLAZA WHK";
                }
                else if (isWhHo && _eJenisDc != EJenisDc.IPLAZA) {
                    exception = "Silahkan Gunakan DC HO Untuk Akses Ke DC Selain IPLAZA WHK";
                }

                if (!string.IsNullOrEmpty(exception)) {
                    throw new TidakMemenuhiException(exception);
                }
            }
        }

    }

}