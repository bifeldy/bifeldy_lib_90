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

        public CSqlite(
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

        public override async Task<int> BulkInsertInto(string tableName, IDataReader dataReader, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            int result = 0;

            string[] columns = [.. Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName)];

            string columnList = string.Join(", ", columns);
            string parameterList = string.Join(", ", columns.Select(c => $"@{c}"));

            string sql = $@"
                INSERT INTO {tableName} ({columnList})
                VALUES ({parameterList})
            ";

            SqliteTransaction transaction = null;

            try {
                this.OpenConnection();

                await using (var pragmaCmd = (SqliteCommand)this.DbConnection.CreateCommand()) {
                    pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
                    _ = await pragmaCmd.ExecuteNonQueryAsync(token);
                }

                transaction = ((SqliteConnection)this.DbConnection).BeginTransaction();

                await using (var cmd = (SqliteCommand)this.DbConnection.CreateCommand()) {
                    cmd.CommandText = sql;
                    cmd.Transaction = transaction;
                    cmd.CommandTimeout = commandTimeoutSeconds;

                    var sqliteParams = new SqliteParameter[dataReader.FieldCount];
                    for (int i = 0; i < dataReader.FieldCount; i++) {
                        sqliteParams[i] = new SqliteParameter(columns[i], DBNull.Value);
                        _ = cmd.Parameters.Add(sqliteParams[i]);
                    }

                    while (dataReader.Read()) {
                        token.ThrowIfCancellationRequested();

                        for (int i = 0; i < dataReader.FieldCount; i++) {
                            sqliteParams[i].Value = dataReader.GetValue(i) ?? DBNull.Value;
                        }

                        _ = await cmd.ExecuteNonQueryAsync(token);

                        result++;
                    }
                }

                await transaction.CommitAsync(token);

                return result;
            }
            catch (Exception ex) {
                if (transaction != null) {
                    try {
                        await transaction.RollbackAsync(token);
                    }
                    catch (Exception rollEx) {
                        this._logger.LogError("[SQLITE_BULK_INSERT_ROLLBACK] {ex}", rollEx.Message);
                    }
                }

                this._logger.LogError("[SQLITE_BULK_INSERT_ERROR] {ex}", ex.Message);
                throw;
            }
            finally {
                if (transaction != null) {
                    await transaction.DisposeAsync();
                }

                this.TryCloseConnection();
            }
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