using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Dapper;

namespace bifeldy_lib_90.Repositories {

    public interface IApiKeyRepository {
        Task<bool> Create(IDatabase db, API_KEY_T apiKey);
        Task<List<API_KEY_T>> GetAll(IDatabase db, string key = null);
        Task<API_KEY_T> GetByKey(IDatabase db, string key);
        Task<bool> Delete(IDatabase db, string key);
        Task<API_KEY_T> SecretLogin(IDatabase db, string key);
        Task<bool> CheckKeyOrigin(IDatabase db, string ipOrigin, string key);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;

        public CApiKeyRepository(IApplicationService @as, IGlobalService gs) {
            this._as = @as;
            this._gs = gs;
        }

        public async Task<bool> Create(IDatabase db, API_KEY_T apiKey) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("ip_origin", apiKey.IP_ORIGIN);
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("keter", apiKey.KETER);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO api_key_t (ip_origin, app_name, keter)
                    VALUES (:ip_origin, :app_name, :keter)
                ",
                sqlParam
            );

            return res > 0;
        }

        public Task<List<API_KEY_T>> GetAll(IDatabase db, string key = null) {
            string sqlQuery = "SELECT * FROM api_key_t WHERE (app_name = '*' OR UPPER(app_name) = :app_name)";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());

            if (!string.IsNullOrEmpty(key)) {
                sqlQuery += " AND UPPER(key) = :key";
                sqlParam.Add("key", key.ToUpper());
            }

            return db.GetListAsync(API_KEY_T_JsonSerializerContext.Default.API_KEY_T, sqlQuery, sqlParam);
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