using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Handlers;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Dapper;

namespace bifeldy_lib_90.Repositories {

    public interface IApiKeyRepository {
        Task<bool> Create(IDatabase db, API_KEY_T apiKey);
        Task<(List<API_KEY_T>, decimal, decimal)> GetAll(IDatabase db, string q = null, string page = "1", string row = "10", string sort = "ip_origin", string order = "asc");
        Task<API_KEY_T> GetByKey(IDatabase db, string key);
        Task<bool> Update(IDatabase db, API_KEY_T apiKey);
        Task<bool> Delete(IDatabase db, string key);
        Task<API_KEY_T> SecretLogin(IDatabase db, string key);
        Task<bool> CheckKeyOrigin(IDatabase db, string ipOrigin, string key);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IApplicationService _as;
        private readonly IChiperService _chiper;
        private readonly IGlobalService _gs;
        private readonly IDefaultHandler _defaultHandler;

        public CApiKeyRepository(
            IApplicationService @as,
            IChiperService chiper,
            IGlobalService gs,
            IDefaultHandler defaultHandler
        ) {
            this._as = @as;
            this._chiper = chiper;
            this._gs = gs;
            this._defaultHandler = defaultHandler;
        }

        public async Task<bool> Create(IDatabase db, API_KEY_T apiKey) {
            apiKey.KEY = this._chiper.HashText($"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}");

            var sqlParam = new DynamicParameters();
            sqlParam.Add("key", apiKey.KEY);
            sqlParam.Add("ip_origin", apiKey.IP_ORIGIN);
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("keter", apiKey.KETER);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO api_key_t (key, ip_origin, app_name, keter)
                    VALUES (:key, :ip_origin, :app_name, :keter)
                ",
                sqlParam
            );

            return res > 0;
        }

        public Task<(List<API_KEY_T>, decimal, decimal)> GetAll(
            IDatabase db,
            string q = null,
            string page = "1",
            string row = "10",
            string sort = "ip_origin",
            string order = "asc"
        ) {
            string sqlQuery = @"
                SELECT *
                FROM api_key_t
                WHERE
                    (app_name = '*' OR UPPER(app_name) = :app_name)
                    AND (
                        ip_origin ILIKE :search_query
                        OR keter ILIKE :search_query
                    )
            ";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("search_query", $"%{q}%");

            return this._defaultHandler.GetListDataPaging(
                db, sqlQuery, sqlParam,
                API_KEY_T_JsonSerializerContext.Default.API_KEY_T,
                page, row, sort, order
            );
        }

        public Task<API_KEY_T> GetByKey(IDatabase db, string key) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("key", key.ToUpper());

            return db.ExecScalarAsync(
                API_KEY_T_JsonSerializerContext.Default.API_KEY_T,
                @"
                    SELECT * FROM api_key_t
                    WHERE (app_name = '*' OR UPPER(app_name) = :app_name) AND UPPER(key) = :key
                ",
                sqlParam
            );
        }

        public async Task<bool> Update(IDatabase db, API_KEY_T apiKey) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("ip_origin", apiKey.IP_ORIGIN);
            sqlParam.Add("keter", apiKey.KETER);
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("key", apiKey.KEY.ToUpper());

            int res = await db.ExecQueryWithResultAsync(
                @"
                    UPDATE api_key_t
                    SET ip_origin = :ip_origin, keter = :keter
                    WHERE UPPER(app_name) = :app_name AND UPPER(key) = :key
                ",
                sqlParam
            );

            return res > 0;
        }

        public async Task<bool> Delete(IDatabase db, string key) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("key", key.ToUpper());

            int res = await db.ExecQueryWithResultAsync(
                @"
                    DELETE FROM api_key_t
                    WHERE UPPER(app_name) = :app_name AND UPPER(key) = :key
                ",
                sqlParam
            );

            return res > 0;
        }

        /* ** */

        public Task<API_KEY_T> SecretLogin(IDatabase db, string key) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("key", key.ToUpper());

            return db.ExecScalarAsync(
                API_KEY_T_JsonSerializerContext.Default.API_KEY_T,
                @"
                    SELECT * FROM api_key_t
                    WHERE ip_origin = '*' AND app_name = '*' AND UPPER(key) = :key
                ",
                sqlParam
            );
        }

        public async Task<bool> CheckKeyOrigin(IDatabase db, string ipOrigin, string key) {
            API_KEY_T ak = await this.GetByKey(db, key);
            return ak != null
                ? ak.IP_ORIGIN.ToUpper().Split(";").Select(io => io.Trim()).Contains(ipOrigin.ToUpper()) || ak.IP_ORIGIN == "*"
                : this._gs.AllowedIpOrigin.Contains(ipOrigin);
        }

    }

}