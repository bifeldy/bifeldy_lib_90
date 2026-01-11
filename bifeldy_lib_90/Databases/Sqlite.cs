using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace bifeldy_lib_90.Databases {

    public interface ISqlite : IDatabase {
        CSqlite NewExternalConnection(string dbName);
        CSqlite CloneConnection();
    }

    public sealed class CSqlite : CDatabase, ISqlite {

        private string DbName { get; set; }

        public CSqlite (
            IOptions<EnvVar> envVar,
            ILogger<CSqlite> logger,
            IApplicationService @as,
            IGlobalService gs,
            IHttpContextAccessor hca
        ) : base(envVar, logger, @as, gs, hca) {
            this.InitializeConnection();
        }

        public void InitializeConnection(string dbName = null) {
            this.DbName = dbName ?? Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, $"{this._as.AppName}.db");

            string _dbConnectionString = $"Data Source={this.DbName}";
            this.DbConnection = new SqliteConnection(_dbConnectionString);
        }

        public override async Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            int result = 0;
            Exception exception = null;

            try {
                if (string.IsNullOrEmpty(tableName)) {
                    throw new Exception("Target Tabel Tidak Ditemukan");
                }

                int colCount = dataTable.Columns.Count;

                var types = new Type[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                string sqlQuery = $"SELECT * FROM {tableName} WHERE 1 = 0";
                await using (var rdr = (SqliteDataReader)await this.ExecReaderAsync(sqlQuery, null, commandTimeoutSeconds, CommandBehavior.Default, token)) {
                    if (rdr.FieldCount != colCount) {
                        throw new Exception("Jumlah Kolom Tabel Tidak Sama");
                    }

                    DataColumnCollection columns = rdr.GetSchemaTable().Columns;
                    for (int i = 0; i < colCount; i++) {
                        types[i] = columns[i].DataType;
                        lengths[i] = columns[i].MaxLength;
                        fieldNames[i] = columns[i].ColumnName;
                    }
                }

                string sbHeader = string.Empty;
                for (int c = 0; c < colCount; c++) {
                    if (!string.IsNullOrEmpty(sbHeader)) {
                        sbHeader += ", ";
                    }

                    sbHeader += fieldNames[c];
                }

                for (int r = 0; r < dataTable.Rows.Count; r++) {
                    var param = new List<SqliteParameter>();

                    string sbColumn = string.Empty;
                    for (int c = 0; c < colCount; c++) {
                        if (!string.IsNullOrEmpty(sbColumn)) {
                            sbColumn += ", ";
                        }

                        string paramKey = $"{fieldNames[c]}_{r}";

                        sbColumn += $":{paramKey}";
                        param.Add(new SqliteParameter(paramKey, dataTable.Rows[r][fieldNames[c]]));
                    }

                    sqlQuery = $"INSERT INTO {tableName} ({sbHeader}) VALUES ({sbColumn})";

                    await using (var cmd = (SqliteCommand)this.DbConnection.CreateCommand()) {
                        cmd.CommandText = sqlQuery;
                        cmd.CommandTimeout = commandTimeoutSeconds;
                        cmd.Parameters.AddRange([.. param]);

                        int run = await cmd.ExecuteNonQueryAsync(token);
                        if (run <= 0) {
                            throw new Exception("Gagal Insert Data");
                        }

                        result++;
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[PG_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                this.TryCloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public CSqlite NewExternalConnection(string dbName) {
            var sqlite = (CSqlite)this.Clone();
            sqlite.InitializeConnection(dbName);
            return sqlite;
        }

        public CSqlite CloneConnection() {
            var sqlite = (CSqlite)this.Clone();
            sqlite.InitializeConnection(this.DbName);
            return sqlite;
        }

    }

}
