using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.TableView;
using Dapper;

namespace bifeldy_lib_90.Repositories {

    public interface IUserRepository {
        Task<bool> Create(IDatabase db, DC_USER_T user);
        Task<IEnumerable<DC_USER_T>> GetAll(IDatabase db, string userNameNik = null);
        Task<DC_USER_T> GetByUserNik(IDatabase db, string userNik);
        Task<DC_USER_T> GetByUserName(IDatabase db, string userName);
        Task<DC_USER_T> GetByUserNameNik(IDatabase db, string userNameNik);
        Task<DC_USER_T> GetByUserNameNikPassword(IDatabase db, string userNameNik, string password);
        Task<bool> Delete(IDatabase db, string userNik);
    }

    public sealed class CUserRepository : CRepository, IUserRepository {

        public CUserRepository() {
            //
        }

        public async Task<bool> Create(IDatabase db, DC_USER_T user) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("user_name", user.USER_NAME);
            sqlParameters.Add("user_password", user.USER_PASSWORD);
            sqlParameters.Add("user_app_modul", user.USER_APP_MODUL);
            sqlParameters.Add("user_privs", user.USER_PRIVS);
            sqlParameters.Add("user_group", user.USER_GROUP);
            sqlParameters.Add("user_fk_tbl_dcid", user.USER_FK_TBL_DCID);
            sqlParameters.Add("user_updrec_date", user.USER_UPDREC_DATE);
            sqlParameters.Add("user_updrec_id", user.USER_UPDREC_ID);
            sqlParameters.Add("user_fk_tbl_lokasiid", user.USER_FK_TBL_LOKASIID);
            sqlParameters.Add("user_fk_tbl_gudangid", user.USER_FK_TBL_GUDANGID);
            sqlParameters.Add("user_fk_tbl_depoid", user.USER_FK_TBL_DEPOID);
            sqlParameters.Add("user_flag_handheld", user.USER_FLAG_HANDHELD);
            sqlParameters.Add("user_nik", user.USER_NIK);
            sqlParameters.Add("user_flag_ho", user.USER_FLAG_HO);
            sqlParameters.Add("last_pass_change", user.LAST_PASS_CHANGE);
            sqlParameters.Add("pass_valid_days", user.PASS_VALID_DAYS);

            int res = await db.ExecQueryWithResultAsync(
                @"
                    INSERT INTO dc_user_t (
                        user_name, user_password, user_app_modul, user_privs, user_group,
                        user_fk_tbl_dcid, user_updrec_date, user_updrec_id, user_fk_tbl_lokasiid,
                        user_fk_tbl_gudangid, user_fk_tbl_depoid, user_flag_handheld, user_nik,
                        user_flag_ho, last_pass_change, pass_valid_days
                    )
                    VALUES (
                        :user_name, :user_password, :user_app_modul, :user_privs, :user_group,
                        :user_fk_tbl_dcid, :user_updrec_date, :user_updrec_id, :user_fk_tbl_lokasiid,
                        :user_fk_tbl_gudangid, :user_fk_tbl_depoid, :user_flag_handheld, :user_nik,
                        :user_flag_ho, :last_pass_change, :pass_valid_days
                    )
                ",
                sqlParameters
            );

            return res > 0;
        }

        public async Task<IEnumerable<DC_USER_T>> GetAll(IDatabase db, string userNameNik = null) {
            string sqlQuery = "SELECT * FROM dc_user_t";

            var sqlParameters = new DynamicParameters();
            if (!string.IsNullOrEmpty(userNameNik)) {
                sqlQuery += " WHERE UPPER(user_name) = :userNameNik OR UPPER(user_nik) = :userNameNik";
                sqlParameters.Add("userNameNik", userNameNik.ToUpper());
            }

            return await db.GetEnumerableAsync(DC_USER_T_JsonSerializerContext.Default.DC_USER_T, sqlQuery, sqlParameters);
        }

        public async Task<DC_USER_T> GetByUserNik(IDatabase db, string userNik) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("user_nik", userNik.ToUpper());

            return await db.GetSingleAsync(
                DC_USER_T_JsonSerializerContext.Default.DC_USER_T,
                @"
                    SELECT * FROM dc_user_t
                    FROM UPPER(user_nik) = :user_nik
                ",
                sqlParameters
            );
        }

        public async Task<DC_USER_T> GetByUserName(IDatabase db, string userName) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("user_name", userName.ToUpper());

            return await db.GetSingleAsync(
                DC_USER_T_JsonSerializerContext.Default.DC_USER_T,
                @"
                    SELECT * FROM dc_user_t
                    WHERE UPPER(user_name) = :user_name
                ",
                sqlParameters
            );
        }

        public async Task<DC_USER_T> GetByUserNameNik(IDatabase db, string userNameNik) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("userNameNik", userNameNik.ToUpper());

            return await db.GetSingleAsync(
                DC_USER_T_JsonSerializerContext.Default.DC_USER_T,
                @"
                    SELECT * FROM dc_user_t
                    WHERE (UPPER(user_name) = :userNameNik OR UPPER(user_nik) = :userNameNik)
                        AND UPPER(user_name) IS NOT NULL AND UPPER(user_nik) IS NOT NULL
                ",
                sqlParameters
            );
        }

        public async Task<DC_USER_T> GetByUserNameNikPassword(IDatabase db, string userNameNik, string password) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("userNameNik", userNameNik.ToUpper());
            sqlParameters.Add("password", password.ToUpper());

            return await db.GetSingleAsync(
                DC_USER_T_JsonSerializerContext.Default.DC_USER_T,
                @"
                    SELECT * FROM dc_user_t
                    WHERE (UPPER(user_name) = :userNameNik OR UPPER(user_nik) = :userNameNik)
                        AND UPPER(user_name) IS NOT NULL AND UPPER(user_nik) IS NOT NULL
                        AND UPPER(user_password) = :password
                ",
                sqlParameters
            );
        }

        public async Task<bool> Delete(IDatabase db, string userNik) {
            var sqlParameters = new DynamicParameters();
            sqlParameters.Add("user_nik", userNik.ToUpper());

            int res = await db.ExecQueryWithResultAsync(
                @"
                    DELETE FROM dc_user_t
                    FROM UPPER(user_nik) = :user_nik
                ",
                sqlParameters
            );

            return res > 0;
        }

    }

}