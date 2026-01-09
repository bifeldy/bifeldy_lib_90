using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Abstractions {

    public interface IDatabase {
        string DbConnectionString { get; }
        object Clone();
        void OpenConnection();
        void TryCloseConnection(bool forceCloseConnection = false);
        DbTransaction TransactionStartAndOpen(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        void TransactionCommitAndClose(DbTransaction useTrx = null, bool forceCloseConnection = false);
        void TransactionRollbackAndClose(DbTransaction useTrx = null, bool forceCloseConnection = false);
        IAsyncEnumerable<T> GetAsyncEnumerable<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        IAsyncEnumerable<T> GetAsyncEnumerable<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<IEnumerable<T>> GetEnumerableAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        Task<IEnumerable<T>> GetEnumerableAsync<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<T> ExecScalarAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        Task<T> ExecScalarAsync<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600);
        Task<int> ExecQueryWithResultAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600);
        Task<bool> ExecQueryAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false);
        Task<DynamicParameters> ExecProcedureAsync(string procedureName, DynamicParameters procedureParameter = null, int commandTimeoutSeconds = 3600);
        Task<DbDataReader> ExecReaderAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default);
        Task<List<string>> RetrieveBlob(string sqlQuery, string stringPathDownload, string stringFileName = null, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default);
        Task<string> BulkGetCsv(string sqlQuery, string delimiter, string filename, string outputFolderPath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default);
        IAsyncEnumerable<int> BulkInsertInto<T>(string tableName, IEnumerable<T> dataListArray, JsonTypeInfo<T> jsonTypeInfo, int chunkSize = 2048, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default);
    }

    public abstract partial class CDatabase : IDatabase, ICloneable {

        protected readonly EnvVar _envVar;

        protected readonly ILogger<CDatabase> _logger;
        protected readonly IApplicationService _as;
        protected readonly IGlobalService _gs;
        protected readonly IHttpContextAccessor _hca;

        protected DbConnection DbConnection;
        protected DbTransaction DbTransaction;

        public string DbConnectionString => this.DbConnection?.ConnectionString;

        public CDatabase(
            IOptions<EnvVar> envVar,
            ILogger<CDatabase> logger,
            IApplicationService @as,
            IGlobalService gs,
            IHttpContextAccessor hca
        ) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._gs = gs;
            this._hca = hca;
        }

        public object Clone() => this.MemberwiseClone();

        public void OpenConnection() {
            if (this.DbTransaction == null) {
                if (this.DbConnection.State != ConnectionState.Closed) {
                    throw new Exception("Koneksi Database Sedang Digunakan");
                }

                this.DbConnection.Open();
            }
        }

        /// <summary> Jangan Lupa Di Commit Atau Rollback Sebelum Menjalankan Ini </summary>
        public void TryCloseConnection(bool forceCloseConnection = false) {
            if (this.DbTransaction != null && forceCloseConnection) {
                this.DbTransaction.Rollback();
                this.DbTransaction.Dispose();
                this.DbTransaction = null;
            }

            if (this.DbConnection.State != ConnectionState.Closed) {
                this.DbConnection.Close();
            }
        }

        public DbTransaction TransactionStartAndOpen(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) {
            this.OpenConnection();
            return this.DbTransaction ??= this.DbConnection.BeginTransaction(isolationLevel);
        }

        public void TransactionCommitAndClose(DbTransaction useTrx = null, bool forceCloseConnection = false) {
            (useTrx ?? this.DbTransaction)?.Commit();
            this.TryCloseConnection(forceCloseConnection);
        }

        public void TransactionRollbackAndClose(DbTransaction useTrx = null, bool forceCloseConnection = false) {
            (useTrx ?? this.DbTransaction)?.Rollback();
            this.TryCloseConnection(forceCloseConnection);
        }

        public virtual async IAsyncEnumerable<T> GetAsyncEnumerable<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, [EnumeratorCancellation] CancellationToken token = default) where T : JsonSerDe, new() {
            try {
                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    await foreach (T item in dr.ToAsyncEnumerable(typeInfo, null, token)) {
                        yield return item;
                    }
                }
            }
            finally {
                this.TryCloseConnection();
            }
        }

        public virtual async IAsyncEnumerable<T> GetAsyncEnumerable<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, [EnumeratorCancellation] CancellationToken token = default) {
            try {
                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    await foreach (T item in dr.ToAsyncEnumerable<T>(null, token)) {
                        yield return item;
                    }
                }
            }
            finally {
                this.TryCloseConnection();
            }
        }

        public virtual async Task<IEnumerable<T>> GetEnumerableAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new() {
            var ls = new List<T>();
            
            IAsyncEnumerable<T> iae = this.GetAsyncEnumerable(typeInfo, sqlQuery, sqlParameter, commandTimeoutSeconds, token);
            await foreach (T item in iae) {
                ls.Add(item);
            }

            return ls;
        }

        public virtual async Task<IEnumerable<T>> GetEnumerableAsync<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var ls = new List<T>();

            IAsyncEnumerable<T> iae = this.GetAsyncEnumerable<T>(sqlQuery, sqlParameter, commandTimeoutSeconds, token);
            await foreach (T item in iae) {
                ls.Add(item);
            }

            return ls;
        }

        public virtual async Task<T> ExecScalarAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new() {
            try {
                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    if (await dr.ReadAsync(token)) {
                        var obj = new T();

                        foreach (JsonPropertyInfo p in typeInfo.Properties) {
                            int idx;

                            try {
                                idx = dr.GetOrdinal(p.Name);
                            }
                            catch {
                                continue;
                            }

                            if (!await dr.IsDBNullAsync(idx, token)) {
                                object value = DbDataReaderExtension.ReadValue(dr, idx, p.PropertyType);
                                p.Set(obj, value);
                            }
                        }

                        return obj;
                    }
                }

                return default;
            }
            finally {
                this.TryCloseConnection();
            }
        }

        // https://github.com/npgsql/npgsql/issues/1663
        public virtual async Task<T> ExecScalarAsync<T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600) {
            try {
                this.OpenConnection();
                // Harus di tahan pakai `async/await` biar tidak balapan dengan `finally`
                return await this.DbConnection.ExecuteScalarAsync<T>(sqlQuery, sqlParameter, this.DbTransaction, commandTimeoutSeconds, CommandType.Text);
            }
            finally {
                this.TryCloseConnection();
            }
        }

        // https://github.com/npgsql/npgsql/issues/1663
        public virtual async Task<int> ExecQueryWithResultAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600) {
            try {
                this.OpenConnection();
                // Harus di tahan pakai `async/await` biar tidak balapan dengan `finally`
                return await this.DbConnection.ExecuteAsync(sqlQuery, sqlParameter, this.DbTransaction, commandTimeoutSeconds, CommandType.Text);
            }
            finally {
                this.TryCloseConnection();
            }
        }

        public virtual async Task<bool> ExecQueryAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false) {
            int affectedRows = await this.ExecQueryWithResultAsync(sqlQuery, sqlParameter, commandTimeoutSeconds);
            return shouldEqualMinRowsAffected ? affectedRows == minRowsAffected : affectedRows >= minRowsAffected;
        }

        public virtual async Task<DynamicParameters> ExecProcedureAsync(string procedureName, DynamicParameters procedureParameter = null, int commandTimeoutSeconds = 3600) {
            try {
                this.OpenConnection();

                _ = await this.DbConnection.ExecuteAsync(
                    procedureName,
                    procedureParameter,
                    this.DbTransaction,
                    commandTimeoutSeconds,
                    CommandType.StoredProcedure
                );

                return procedureParameter;
            }
            finally {
                this.TryCloseConnection();
            }
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        /// <summary> Saat Setelah Selesai Baca Dan Tidak Digunakan Lagi </summary>
        /// <summary> Bisa Pakai Manual Panggil Fungsi Close / Commit / Rollback Di Atas </summary>
        public virtual Task<DbDataReader> ExecReaderAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default) {
            this.OpenConnection();

            var commandDefinition = new CommandDefinition(
                sqlQuery,
                sqlParameter,
                this.DbTransaction,
                commandTimeoutSeconds,
                CommandType.Text,
                cancellationToken: token
            );

            return this.DbConnection.ExecuteReaderAsync(commandDefinition, commandBehavior);
        }

        public virtual async Task<List<string>> RetrieveBlob(string sqlQuery, string stringPathDownload, string stringFileName = null, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default) {
            var result = new List<string>();
            Exception exception = null;

            try {
                string _sqlQuery = $"SELECT COUNT(*) FROM ( {sqlQuery} ) RetrieveBlob_{DateTime.Now.Ticks}";
                ulong? _totalFiles = await this.ExecScalarAsync<ulong?>(_sqlQuery, sqlParameter, commandTimeoutSeconds);
                if (_totalFiles <= 0) {
                    throw new Exception("File Tidak Ditemukan");
                }

                using (DbDataReader rdrGetBlob = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, CommandBehavior.SequentialAccess, token)) {
                    if (string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount != 2) {
                        throw new Exception($"Jika Nama File Kosong Maka Harus Berjumlah 2 Kolom{Environment.NewLine}SELECT kolom_blob_data, kolom_nama_file FROM ...");
                    }
                    else if (!string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount > 1) {
                        throw new Exception($"Harus Berjumlah 1 Kolom{Environment.NewLine}SELECT kolom_blob_data FROM ...");
                    }

                    int bufferSize = 1024;
                    byte[] outByte = new byte[bufferSize];

                    while (await rdrGetBlob.ReadAsync(token)) {
                        string filePath = Path.Combine(stringPathDownload, stringFileName);

                        if (rdrGetBlob.FieldCount == 2) {
                            string fileMultipleName = rdrGetBlob.GetString(1);
                            if (string.IsNullOrEmpty(fileMultipleName)) {
                                fileMultipleName = $"{DateTime.Now.Ticks}";
                            }

                            filePath = Path.Combine(stringPathDownload, fileMultipleName);
                        }

                        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096)) {
                            using (var bw = new BinaryWriter(fs, encoding ?? Encoding.UTF8)) {
                                long startIndex = 0;
                                long retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);

                                while (retval == bufferSize) {
                                    bw.Write(outByte);
                                    bw.Flush();
                                    startIndex += bufferSize;
                                    retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);
                                }

                                if (retval > 0) {
                                    bw.Write(outByte, 0, (int) retval);
                                }

                                bw.Flush();
                            }
                        }

                        result.Add(filePath);
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[RETRIEVE_BLOB] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                this.TryCloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public virtual async Task<string> BulkGetCsv(string sqlQuery, string delimiter, string filename, string outputFolderPath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default) {
            string result = null;
            Exception exception = null;

            try {
                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, filename);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

                string _sqlQuery = $"SELECT * FROM ( {sqlQuery} ) alias_{DateTime.Now.Ticks}";
                using (DbDataReader rdr = await this.ExecReaderAsync(_sqlQuery, sqlParameter, commandTimeoutSeconds, CommandBehavior.SequentialAccess, token)) {
                    await rdr.ToCsv(delimiter, tempPath, includeHeader, useDoubleQuote, allUppercase, encoding ?? Encoding.UTF8, token);
                }

                string realPath = Path.Combine(outputFolderPath ?? this._gs.CsvFolderPath, filename);
                if (File.Exists(realPath)) {
                    File.Delete(realPath);
                }

                File.Move(tempPath, $"{realPath}.tmp", true);
                File.Move($"{realPath}.tmp", realPath, true);

                result = realPath;
            }
            catch (Exception ex) {
                this._logger.LogError("[BULK_GET_CSV] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                this.TryCloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public virtual async IAsyncEnumerable<int> BulkInsertInto<T>(string tableName, IEnumerable<T> dataListArray, JsonTypeInfo<T> jsonTypeInfo, int chunkSize = 2048, int commandTimeoutSeconds = 3600, [EnumeratorCancellation] CancellationToken token = default) where T : JsonSerDe, new() {
            int batchNumber = 1;
            int totalInserted = 0;

            foreach (T[] chunk in dataListArray.Chunk(chunkSize)) {
                DataTable dt = chunk.ToDataTable(jsonTypeInfo, tableName);

                int res = await this.BulkInsertInto(tableName, dt, commandTimeoutSeconds, chunkSize, token);
                if (res != dt.Rows.Count) {
                    throw new Exception($"Gagal Menyimpan Data (Batch: #{batchNumber} | Sukses: {totalInserted})");
                }

                batchNumber++;
                totalInserted += dt.Rows.Count;

                // Return Completed Each Batch Total DataRowList
                yield return dt.Rows.Count;
            }
        }

        /** Wajib di Override */

        public abstract Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default);

    }

}
