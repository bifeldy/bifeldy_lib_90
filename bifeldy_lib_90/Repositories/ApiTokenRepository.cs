using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Dapper;

namespace bifeldy_lib_90.Repositories {

    public interface IApiTokenRepository {
        Task<bool> Create(IDatabase db, API_TOKEN_T apiToken);
        Task<IEnumerable<API_TOKEN_T>> GetAll(IDatabase db, string userName = null);
        Task<API_TOKEN_T> GetByUserName(IDatabase db, string userName);
        Task<API_TOKEN_T> GetByUserNamePass(IDatabase db, string userName, string password);
        Task<bool> Delete(IDatabase db, string userName);
        Task<bool> CheckTokenSekaliPakaiIsValid(IDatabase db, API_TOKEN_T apiToken, string tokenSekaliPakai);
        Task<API_TOKEN_T> LoginBot(IDatabase db, string userName, string password);
    }

    public sealed class CApiTokenRepository : CRepository, IApiTokenRepository {

        private readonly IApplicationService _as;

        public CApiTokenRepository(IApplicationService @as) {
            this._as = @as;
        }

        public async Task<bool> Create(IDatabase db, API_TOKEN_T apiToken) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("user_name", apiToken.USER_NAME);
            sqlParam.Add("password", apiToken.PASSWORD);
            sqlParam.Add("app_name", apiToken.APP_NAME);
            sqlParam.Add("last_login", apiToken.LAST_LOGIN);
            sqlParam.Add("token_sekali_pakai", apiToken.TOKEN_SEKALI_PAKAI);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO api_token_t (user_name, password, app_name, last_login, token_sekali_pakai)
                    VALUES (:user_name, :password, :app_name, :last_login, :token_sekali_pakai)
                ",
                sqlParam
            );

            return res > 0;
        }

        public Task<IEnumerable<API_TOKEN_T>> GetAll(IDatabase db, string userName = null) {
            string sqlQuery = "SELECT * FROM api_token_t WHERE (app_name = '*' OR UPPER(app_name) = :app_name)";

            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());

            if (!string.IsNullOrEmpty(userName)) {
                sqlQuery += " AND UPPER(user_name) = :user_name";
                sqlParam.Add("user_name", userName.ToUpper());
            }

            return db.GetListAsync(API_TOKEN_T_JsonSerializerContext.Default.API_TOKEN_T, sqlQuery, sqlParam);
        }

        public Task<API_TOKEN_T> GetByUserName(IDatabase db, string userName) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("user_name", userName.ToUpper());

            return db.ExecScalarAsync(
                API_TOKEN_T_JsonSerializerContext.Default.API_TOKEN_T,
                @"
                    SELECT * FROM api_token_t
                    WHERE (app_name = '*' OR UPPER(app_name) = :app_name) AND UPPER(user_name) = :user_name
                ",
                sqlParam
            );
        }

        public Task<API_TOKEN_T> GetByUserNamePass(IDatabase db, string userName, string password) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("user_name", userName.ToUpper());
            sqlParam.Add("password", password.ToUpper());

            return db.ExecScalarAsync(
                API_TOKEN_T_JsonSerializerContext.Default.API_TOKEN_T,
                @"
                    SELECT * FROM api_token_t
                    WHERE (app_name = '*' OR UPPER(app_name) = :app_name)
                        AND UPPER(user_name) = :user_name AND UPPER(password) = :password
                ",
                sqlParam
            );
        }

        public async Task<bool> Delete(IDatabase db, string userName) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("app_name", this._as.AppName.ToUpper());
            sqlParam.Add("user_name", userName.ToUpper());

            int res = await db.ExecQueryWithResultAsync(
                @"
                    DELETE FROM api_token_t
                    WHERE UPPER(app_name) = :app_name AND UPPER(user_name) = :user_name
                ",
                sqlParam
            );

            return res > 0;
        }

        /* ** */

        public async Task<bool> CheckTokenSekaliPakaiIsValid(IDatabase db, API_TOKEN_T apiToken, string tokenSekaliPakai) {
            string token = tokenSekaliPakai.ToUpper();

            bool tokenSekaliPakaiValid = apiToken?.TOKEN_SEKALI_PAKAI.ToUpper() == token;
            if (tokenSekaliPakaiValid) {
                var sqlParam = new DynamicParameters();
                sqlParam.Add("app_name", this._as.AppName.ToUpper());
                sqlParam.Add("user_name", apiToken.USER_NAME.ToUpper());
                sqlParam.Add("token_sekali_pakai", token);

                int res = await db.ExecQueryWithResultAsync(
                    @"
                        UPDATE api_token_t
                        SET token_sekali_pakai = NULL
                        WHERE UPPER(app_name) = :app_name AND UPPER(user_name) = :user_name
                            AND UPPER(token_sekali_pakai) = :token_sekali_pakai
                    ",
                    sqlParam
                );

                tokenSekaliPakaiValid = res > 0;
            }

            return tokenSekaliPakaiValid;
        }

        public async Task<API_TOKEN_T> LoginBot(IDatabase db, string userName, string password) {
            API_TOKEN_T apiToken = await this.GetByUserNamePass(db, userName, password);
            if (apiToken != null) {
                var sqlParam = new DynamicParameters();
                sqlParam.Add("app_name", this._as.AppName.ToUpper());
                sqlParam.Add("user_name", apiToken.USER_NAME.ToUpper());

                int res = await db.ExecQueryWithResultAsync(
                    @"
                        UPDATE api_token_t
                        SET token_sekali_pakai = NULL, LAST_LOGIN = CURRENT_TIMESTAMP
                        WHERE UPPER(app_name) = :app_name AND UPPER(user_name) = :user_name
                    ",
                    sqlParam
                );

                if (res > 0) {
                    return await this.GetByUserNamePass(db, userName, password);
                }
            }

            return null;
        }

    }

}
