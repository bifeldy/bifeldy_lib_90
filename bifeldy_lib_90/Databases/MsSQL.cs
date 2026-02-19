using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace bifeldy_lib_90.Databases {

    public interface IMsSQL : IDatabase {
        CMsSQL NewExternalConnection(string dbIpAddrss, string dbUsername, string dbPassword, string dbName);
        CMsSQL CloneConnection();
    }

    public sealed class CMsSQL : CDatabase, IMsSQL {

        private readonly IServerConfigRepository _scr;

        private string DbIpAddrss { get; set; }
        private string DbName { get; set; }
        private string DbUsername { get; set; }
        private string DbPassword { get; set; }

        public CMsSQL(
            IOptions<EnvVar> envVar,
            ILogger<CMsSQL> logger,
            IApplicationService @as,
            IGlobalService gs,
            IHttpContextAccessor hca,
            IServerConfigRepository scr
        ) : base(envVar, logger, @as, gs, hca) {
            this._scr = scr;
            this.InitializeConnection();
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbName = null, string dbUsername = null, string dbPassword = null) {
            string kunciGxxx = null;

            if (this._hca.HttpContext != null) {
                kunciGxxx = this._hca.HttpContext.Items["kunci_gxxx"]?.ToString();
            }

            kunciGxxx ??= this._scr.CurrentLoadedKodeServerKunciDc();

            this.DbIpAddrss = dbIpAddrss ?? this._as.GetVariabel("IPSql", kunciGxxx);
            this.DbName = dbName ?? this._as.GetVariabel("DatabaseSql", kunciGxxx);
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserSql", kunciGxxx);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordSql", kunciGxxx);

            string _dbConnectionString = $"Data Source={this.DbIpAddrss};Initial Catalog={this.DbName};User ID={this.DbUsername};Password={this.DbPassword};Connection Timeout=180;"; // 3 Minutes
            this.DbConnection = new SqlConnection(_dbConnectionString);
        }

        public override async Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            int result = 0;
            Exception exception = null;

            try {
                this.OpenConnection();
                using (var dbBulkCopy = new SqlBulkCopy((SqlConnection)this.DbConnection) {
                    DestinationTableName = tableName,
                    BatchSize = chunkSize
                }) {
                    await dbBulkCopy.WriteToServerAsync(dataTable, token);
                    result = dataTable.Rows.Count;
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[SQL_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                this.TryCloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public CMsSQL NewExternalConnection(string dbIpAddrss, string dbUsername, string dbPassword, string dbName) {
            var mssql = (CMsSQL)this.Clone();
            mssql.InitializeConnection(dbIpAddrss, dbUsername, dbPassword, dbName);
            return mssql;
        }

        public CMsSQL CloneConnection() {
            var mssql = (CMsSQL)this.Clone();
            mssql.InitializeConnection(this.DbIpAddrss, this.DbUsername, this.DbPassword, this.DbName);
            return mssql;
        }

    }

}