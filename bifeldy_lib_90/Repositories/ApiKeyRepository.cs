using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Dapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Diagnostics.Metrics;

namespace bifeldy_lib_90.Repositories {

    public interface IApiKeyRepository {
        Task<bool> Create(bool isPg, IDatabase db, API_KEY_T apiKey);
        Task<IEnumerable<API_KEY_T>> GetAll(bool isPg, IDatabase db, string key = null);
        Task<API_KEY_T> GetByKey(bool isPg, IDatabase db, string key);
        Task<bool> Delete(bool isPg, IDatabase db, string key);
        Task<API_KEY_T> SecretLogin(bool isPg, IDatabase db, string key);
        Task<bool> CheckKeyOrigin(bool isPg, IDatabase db, string ipOrigin, string key);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;

        public CApiKeyRepository(IApplicationService @as, IGlobalService gs) {
            this._as = @as;
            this._gs = gs;
        }

        public async Task<bool> Create(bool isPg, IDatabase db, API_KEY_T apiKey) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("ip_origin", apiKey.IP_ORIGIN);
            sqlParameters.Add("app_name", this._as.AppName.ToUpper());
            sqlParameters.Add("keter", apiKey.KETER);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO api_key_t (ip_origin, app_name, keter)
                    VALUES (:ip_origin, :app_name, :keter)
                ",
                sqlParameters
            );

            return res > 0;
        }

        public async Task<IEnumerable<API_KEY_T>> GetAll(bool isPg, IDatabase db, string key = null) {
            string sqlQuery = "SELECT * FROM api_key_t WHERE app_name = '*' OR (UPPER(app_name) = :app_name)";

            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("app_name", this._as.AppName.ToUpper());

            if (!string.IsNullOrEmpty(key)) {
                sqlQuery += " AND UPPER(key) = :key";
                sqlParameters.Add("key", key.ToUpper());
            }

            sqlQuery += ")";

            return await db.GetEnumerableAsync(API_KEY_T_JsonSerializerContext.Default.API_KEY_T, sqlQuery, sqlParameters);
        }

        public async Task<API_KEY_T> GetByKey(bool isPg, IDatabase db, string key) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("app_name", this._as.AppName.ToUpper());
            sqlParameters.Add("key", key.ToUpper());

            return await db.GetSingleAsync(
                API_KEY_T_JsonSerializerContext.Default.API_KEY_T,
                @"
                    SELECT * FROM api_key_t
                    WHERE UPPER(app_name) = :app_name AND UPPER(key) = :key
                ",
                sqlParameters
            );
        }

        public async Task<bool> Delete(bool isPg, IDatabase db, string key) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("app_name", this._as.AppName.ToUpper());
            sqlParameters.Add("key", key.ToUpper());

            int res = await db.ExecQueryWithResultAsync(
                @"
                    DELETE FROM api_key_t
                    WHERE UPPER(app_name) = :app_name AND UPPER(key) = :key
                ",
                sqlParameters
            );

            return res > 0;
        }

        /* ** */

        public async Task<API_KEY_T> SecretLogin(bool isPg, IDatabase db, string key) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("key", key.ToUpper());

            return await db.GetSingleAsync(
                API_KEY_T_JsonSerializerContext.Default.API_KEY_T,
                @"
                    SELECT * FROM api_key_t
                    WHERE ip_origin = '*' AND app_name = '*' AND UPPER(key) = :key
                ",
                sqlParameters
            );
        }

        public async Task<bool> CheckKeyOrigin(bool isPg, IDatabase db, string ipOrigin, string key) {
            API_KEY_T ak = await this.GetByKey(isPg, db, key);
            return ak != null
                ? ak.IP_ORIGIN.ToUpper().Split(";").Select(io => io.Trim()).Contains(ipOrigin.ToUpper()) || ak.IP_ORIGIN == "*"
                : this._gs.AllowedIpOrigin.Contains(ipOrigin);
        }

    }

}