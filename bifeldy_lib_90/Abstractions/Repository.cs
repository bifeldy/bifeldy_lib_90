using bifeldy_lib_90.Models;
using Dapper;

namespace bifeldy_lib_90.Abstractions {

    public interface IRepository {
        Task<string> GetJenisDc(IDatabase db, string kodeDc);
        Task<EJenisDc> GetJenisDc(IDatabase db);
        Task<string> GetKodeDc(IDatabase db);
        Task<string> GetNamaDc(IDatabase db);
        Task<bool> IsDcHo(IDatabase db);
        Task<bool> IsNonDc(IDatabase db);
        Task<bool> IsWhHo(IDatabase db);
        Task<bool> IsHo(IDatabase db);
        Task<bool> IsDc(IDatabase db);
    }

    public abstract class CRepository : IRepository {

        private EJenisDc JenisDc = 0;
        private string KodeDc = null;
        private string NamaDc = null;

        public CRepository() {
            //
        }

        public async Task<string> GetJenisDc(IDatabase db, string kodeDc) {
            string sqlQuery = "SELECT UPPER(tbl_jenis_dc) FROM dc_tabel_dc_t";
            var sqlParam = new DynamicParameters();

            if (!string.IsNullOrEmpty(kodeDc)) {
                sqlQuery += " WHERE UPPER(tbl_dc_kode) = :tbl_dc_kode";
                sqlParam.Add("tbl_dc_kode", kodeDc.ToUpper());
            }

            return await db.ExecScalarAsync<string>(sqlQuery, sqlParam);
        }

        public async Task<EJenisDc> GetJenisDc(IDatabase db) {
            if (this.JenisDc == 0) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (
                        _dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN") ||
                        _dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")
                    ) {
                        this.JenisDc = EJenisDc.NONDC;
                    }
                    else if (_dbConStr.Contains("DCHO") || _dbConStr.Contains("WHHO")) {
                        this.JenisDc = EJenisDc.HO;
                    }
                    else {
                        string jenisDc = await this.GetJenisDc(db, null);

                        if (Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                            this.JenisDc = eJenisDc;
                        }
                        else {
                            throw new Exception("Jenis DC Tidak Valid");
                        }
                    }
                }
            }

            return this.JenisDc;
        }

        public async Task<string> GetKodeDc(IDatabase db) {
            if (string.IsNullOrEmpty(this.KodeDc)) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (_dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN")) {
                        this.KodeDc = "KCBN";
                    }
                    else if (_dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")) {
                        this.KodeDc = "RLTM";
                    }
                    else if(_dbConStr.Contains("DCHO")) {
                        this.KodeDc = "DCHO";
                    }
                    else if (_dbConStr.Contains("WHHO")) {
                        this.KodeDc = "WHHO";
                    }
                    else {
                        this.KodeDc = await db.ExecScalarAsync<string>("SELECT UPPER(tbl_dc_kode) FROM dc_tabel_dc_t");
                    }
                }
            }

            return this.KodeDc?.ToUpper();
        }

        public async Task<string> GetNamaDc(IDatabase db) {
            if (string.IsNullOrEmpty(this.NamaDc)) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (_dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN")) {
                        this.NamaDc = "KONSOLIDASI CBN";
                    }
                    else if (_dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")) {
                        this.NamaDc = "REAL-TIME-SCALE";
                    }
                    else if (_dbConStr.Contains("DCHO")) {
                        this.NamaDc = "DC HEAD OFFICE";
                    }
                    else if (_dbConStr.Contains("WHHO")) {
                        this.NamaDc = "WH HEAD OFFICE";
                    }
                    else {
                        this.NamaDc = await db.ExecScalarAsync<string>("SELECT UPPER(tbl_dc_nama) FROM dc_tabel_dc_t");
                    }
                }
            }

            return this.NamaDc?.ToUpper();
        }

        public async Task<bool> IsNonDc(IDatabase db) {
            EJenisDc jenisDc = await this.GetJenisDc(db);
            return jenisDc == EJenisDc.NONDC;
        }

        public async Task<bool> IsDcHo(IDatabase db) {
            string kodeDc = await this.GetKodeDc(db);
            return kodeDc == "DCHO";
        }

        public async Task<bool> IsWhHo(IDatabase db) {
            string kodeDc = await this.GetKodeDc(db);
            return kodeDc == "WHHO";
        }

        public async Task<bool> IsHo(IDatabase db) {
            EJenisDc jenisDc = await this.GetJenisDc(db);
            return jenisDc == EJenisDc.HO;
        }

        public async Task<bool> IsDc(IDatabase db) {
            bool isNonDc = await this.IsNonDc(db);
            bool isDcHo = await this.IsDcHo(db);
            bool isWhHo = await this.IsWhHo(db);
            bool isHo = await this.IsHo(db);
            return !isNonDc && !isDcHo && !isWhHo && !isHo;
        }

    }

}
