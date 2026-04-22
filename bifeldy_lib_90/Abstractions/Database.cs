using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;

[assembly: DapperAot]

namespace bifeldy_lib_90.Abstractions {

    public interface IDatabase {
        string DbConnectionString { get; }
        object Clone();
        Task OpenConnectionAsync(CancellationToken token = default);
        Task TryCloseConnectionAsync(bool forceCloseConnection = false, CancellationToken token = default);
        Task<DbTransaction> TransactionStartAndOpenAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken token = default);
        Task TransactionCommitAndCloseAsync(DbTransaction useTrx = null, bool forceCloseConnection = false, CancellationToken token = default);
        Task TransactionRollbackAndCloseAsync(DbTransaction useTrx = null, bool forceCloseConnection = false, CancellationToken token = default);
        IAsyncEnumerable<T> GetAsyncEnumerable<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) where T : JsonSerDe, new();
        IAsyncEnumerable<T> GetAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default);
        Task<DataTable> GetDataTableAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default, [CallerMemberName] string callerMemberName = null);
        Task<List<T>> GetListAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) where T : JsonSerDe, new();
        Task<List<T>> GetListAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default);
        Task<T> ExecScalarAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        Task<T> ExecScalarAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<int> ExecQueryWithResultAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<bool> ExecQueryAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, CancellationToken token = default);
        Task<DynamicParameters> ExecProcedureAsync(string procedureName, DynamicParameters procedureParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<DbDataReader> ExecReaderAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default);
        Task<List<string>> RetrieveBlob(string sqlQuery, string stringPathDownload, string stringFileName = null, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default);
        Task<string> BulkGetCsv(string sqlQuery, string delimiter, string filename, string outputFolderPath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default);
        Task<int> BulkInsertInto(string tableName, IDataReader dataReader, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default);
        Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default);
        IAsyncEnumerable<int> BulkInsertIntoIAE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(JsonTypeInfo<T> jsonTypeInfo, string tableName, IEnumerable<T> dataListArray, int chunkSize = 2048, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        IAsyncEnumerable<int> BulkInsertIntoIAE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string tableName, IEnumerable<T> dataListArray, int chunkSize = 2048, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<int> BulkInsertInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(JsonTypeInfo<T> jsonTypeInfo, string tableName, IEnumerable<T> dataListArray, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new();
        Task<int> BulkInsertInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string tableName, IEnumerable<T> dataListArray, int commandTimeoutSeconds = 3600, CancellationToken token = default);
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

        public async Task OpenConnectionAsync(CancellationToken token = default) {
            if (this.DbTransaction == null) {
                if (this.DbConnection.State != ConnectionState.Closed) {
                    throw new Exception("Koneksi Database Sedang Digunakan");
                }

                await this.DbConnection.OpenAsync(token);
            }
        }

        /// <summary> Jangan Lupa Di Commit Atau Rollback Sebelum Menjalankan Ini </summary>
        public async Task TryCloseConnectionAsync(bool forceCloseConnection = false, CancellationToken token = default) {
            if (this.DbTransaction != null && forceCloseConnection) {
                await this.DbTransaction.RollbackAsync(token);
                await this.DbTransaction.DisposeAsync();
                this.DbTransaction = null;
            }

            if (this.DbConnection.State != ConnectionState.Closed) {
                await this.DbConnection.CloseAsync();
            }
        }

        public async Task<DbTransaction> TransactionStartAndOpenAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken token = default) {
            await this.OpenConnectionAsync(token);
            return this.DbTransaction ??= this.DbConnection.BeginTransaction(isolationLevel);
        }

        public async Task TransactionCommitAndCloseAsync(DbTransaction useTrx = null, bool forceCloseConnection = false, CancellationToken token = default) {
            await (useTrx ?? this.DbTransaction)?.CommitAsync(token);
            await this.TryCloseConnectionAsync(forceCloseConnection, token);
        }

        public async Task TransactionRollbackAndCloseAsync(DbTransaction useTrx = null, bool forceCloseConnection = false, CancellationToken token = default) {
            await (useTrx ?? this.DbTransaction)?.RollbackAsync(token);
            await this.TryCloseConnectionAsync(forceCloseConnection, token);
        }

        private async IAsyncEnumerable<T> GetAsyncEnumerableInternal<T>(
            Func<DbDataReader, IAsyncEnumerable<T>> streamSelector,
            string sqlQuery,
            DynamicParameters sqlParameter,
            int commandTimeoutSeconds,
            [EnumeratorCancellation] CancellationToken token
        ) {
            try {
                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    IAsyncEnumerable<T> iae = streamSelector(dr);
                    await foreach (T item in iae.WithCancellation(token)) {
                        yield return item;
                    }
                }
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        public virtual IAsyncEnumerable<T> GetAsyncEnumerable<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) where T : JsonSerDe, new() {
            return this.GetAsyncEnumerableInternal(dr => dr.ToAsyncEnumerable(typeInfo, callback, token), sqlQuery, sqlParameter, commandTimeoutSeconds, token);
        }

        public virtual IAsyncEnumerable<T> GetAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) {
            return this.GetAsyncEnumerableInternal(dr => dr.ToAsyncEnumerable(callback, token), sqlQuery, sqlParameter, commandTimeoutSeconds, token);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Safe primitive types from JsonTypeInfo.")]
        public virtual async Task<DataTable> GetDataTableAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default, [CallerMemberName] string callerMemberName = null) {
            try {
                Console.WriteLine($"[{callerMemberName}] :: Yaah, Pake Data Table - Lambat Broo, Mending Ganti Pakai `Class` Biasa Aja ~ Cobain Deh Bikin Kelas `T` Terus Pakai `GetAsyncEnumerable<T>` Atau `GetListAsync<T>`");

                const int dataTableMaxAllowedRows = 1_000_000;
                var result = new DataTable();

                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    DataTable schema = dr.GetSchemaTable();
                    foreach (DataRow row in schema.Rows) {
                        _ = result.Columns.Add(row["ColumnName"].ToString(), (Type)row["DataType"]);
                    }

                    int rowCount = 0;
                    while (await dr.ReadAsync(token)) {
                        rowCount++;

                        if (rowCount > dataTableMaxAllowedRows) {
                            Console.WriteLine($"[{callerMemberName}] :: Data Terlalu Banyak, Total Rows {rowCount} / Limit Max {dataTableMaxAllowedRows} Rows ~ Masih Bisa Lanjut Sih ..");
                        }

                        DataRow newRow = result.NewRow();
                        for (int i = 0; i < dr.FieldCount; i++) {
                            newRow[i] = dr.GetValue(i);
                        }

                        result.Rows.Add(newRow);
                    }
                }

                if (this._hca.HttpContext != null) {
                    if (this._hca.HttpContext.Items["DisposalBucket"] is not List<IDisposable> bucket) {
                        bucket = [];
                        this._hca.HttpContext.Items["DisposalBucket"] = bucket;
                    }

                    bucket.Add(result);
                }

                return result;
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        public virtual async Task<List<T>> GetListAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) where T : JsonSerDe, new() {
            var ls = new List<T>();

            IAsyncEnumerable<T> iae = this.GetAsyncEnumerable(typeInfo, sqlQuery, sqlParameter, commandTimeoutSeconds, callback, token);
            await foreach (T item in iae.WithCancellation(token)) {
                ls.Add(item);
            }

            return ls;
        }

        public virtual async Task<List<T>> GetListAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Action<T> callback = null, CancellationToken token = default) {
            var ls = new List<T>();

            IAsyncEnumerable<T> iae = this.GetAsyncEnumerable(sqlQuery, sqlParameter, commandTimeoutSeconds, callback, token);
            await foreach (T item in iae.WithCancellation(token)) {
                ls.Add(item);
            }

            return ls;
        }

        public virtual async Task<T> ExecScalarAsync<T>(JsonTypeInfo<T> typeInfo, string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new() {
            try {
                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    if (await dr.ReadAsync(token)) {
                        var obj = new T();

                        for (int i = 0; i < dr.FieldCount; i++) {
                            string colName = dr.GetName(i);
                            JsonPropertyInfo prop = typeInfo.Properties.FirstOrDefault(p => string.Equals(p.Name, colName, StringComparison.OrdinalIgnoreCase));
                            if (prop != null && !await dr.IsDBNullAsync(i, token)) {
                                object value = DbDataReaderExtension.ReadValue(dr, i, prop.PropertyType);
                                prop.Set(obj, value);
                            }
                        }

                        return obj;
                    }

                    return default;
                }
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        public virtual async Task<T> ExecScalarAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            try {
                Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                if (ObjectExtension.IsSimpleType(t)) {
                    await this.OpenConnectionAsync(token);

                    var commandDefinition = new CommandDefinition(
                        sqlQuery,
                        sqlParameter,
                        this.DbTransaction,
                        commandTimeoutSeconds,
                        CommandType.Text,
                        cancellationToken: token
                    );

                    return await this.DbConnection.ExecuteScalarAsync<T>(commandDefinition);
                }

                if (!RuntimeFeature.IsDynamicCodeSupported) {
                    throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
                }

                await using (DbDataReader dr = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token: token)) {
                    var colIndexLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < dr.FieldCount; i++) {
                        colIndexLookup[dr.GetName(i)] = i;
                    }

                    IEnumerable<DbDataReaderExtension.DataReaderMapping> mappings = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite && colIndexLookup.ContainsKey(p.Name))
                        .Select(p => new DbDataReaderExtension.DataReaderMapping(
                            p.PropertyType,
                            ObjectExtension.CreateSetter(p),
                            colIndexLookup[p.Name]
                        ));

                    while (await dr.ReadAsync(token)) {
                        T obj = Activator.CreateInstance<T>();

                        foreach (DbDataReaderExtension.DataReaderMapping m in mappings) {
                            if (!await dr.IsDBNullAsync(m.Index, token)) {
                                object value = DbDataReaderExtension.ReadValue(dr, m.Index, m.TargetType);
                                m.Setter(obj, value);
                            }
                        }

                        return obj;
                    }
                }

                return default;
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        // https://github.com/npgsql/npgsql/issues/1663
        public virtual async Task<int> ExecQueryWithResultAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            try {
                await this.OpenConnectionAsync(token);

                var commandDefinition = new CommandDefinition(
                    sqlQuery,
                    sqlParameter,
                    this.DbTransaction,
                    commandTimeoutSeconds,
                    CommandType.Text,
                    cancellationToken: token
                );

                // Harus di tahan pakai `async/await` biar tidak balapan dengan `finally`
                return await this.DbConnection.ExecuteAsync(commandDefinition);
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        public virtual async Task<bool> ExecQueryAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, CancellationToken token = default) {
            int affectedRows = await this.ExecQueryWithResultAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, token);
            return shouldEqualMinRowsAffected ? affectedRows == minRowsAffected : affectedRows >= minRowsAffected;
        }

        public virtual async Task<DynamicParameters> ExecProcedureAsync(string procedureName, DynamicParameters procedureParameter = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            try {
                await this.OpenConnectionAsync(token);

                var commandDefinition = new CommandDefinition(
                    procedureName,
                    procedureParameter,
                    this.DbTransaction,
                    commandTimeoutSeconds,
                    CommandType.StoredProcedure,
                    cancellationToken: token
                );

                _ = await this.DbConnection.ExecuteAsync(commandDefinition);

                return procedureParameter;
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        /// <summary> Saat Setelah Selesai Baca Dan Tidak Digunakan Lagi </summary>
        /// <summary> Bisa Pakai Manual Panggil Fungsi Close / Commit / Rollback Di Atas </summary>
        public virtual async Task<DbDataReader> ExecReaderAsync(string sqlQuery, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, CommandBehavior commandBehavior = CommandBehavior.Default, CancellationToken token = default) {
            await this.OpenConnectionAsync(token);

            var commandDefinition = new CommandDefinition(
                sqlQuery,
                sqlParameter,
                this.DbTransaction,
                commandTimeoutSeconds,
                CommandType.Text,
                cancellationToken: token
            );

            return await this.DbConnection.ExecuteReaderAsync(commandDefinition, commandBehavior);
        }

        public virtual async Task<List<string>> RetrieveBlob(string sqlQuery, string stringPathDownload, string stringFileName = null, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default) {
            var result = new List<string>();
            Exception exception = null;

            try {
                string _sqlQuery = $"SELECT COUNT(*) FROM ( {sqlQuery} ) RetrieveBlob_{DateTime.Now.Ticks}";
                ulong _totalFiles = await this.ExecScalarAsync<ulong>(_sqlQuery, sqlParameter, commandTimeoutSeconds, token);
                if (_totalFiles <= 0) {
                    throw new Exception("File Tidak Ditemukan");
                }

                await using (DbDataReader rdrGetBlob = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, CommandBehavior.SequentialAccess, token)) {
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

                        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096)) {
                            await using (var bw = new BinaryWriter(fs, encoding ?? Encoding.UTF8)) {
                                long startIndex = 0;
                                long retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);

                                while (retval == bufferSize) {
                                    bw.Write(outByte);
                                    bw.Flush();
                                    startIndex += bufferSize;
                                    retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);
                                }

                                if (retval > 0) {
                                    bw.Write(outByte, 0, (int)retval);
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
                await this.TryCloseConnectionAsync(token: token);
            }

            return (exception == null) ? result : throw exception;
        }

        // Saran :: Kalau Ada Bawaan Library Mending Di Timpa Pakai Nativenya
        public virtual async Task<string> BulkGetCsv(string sqlQuery, string delimiter, string filename, string outputFolderPath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default) {
            try {
                string tempPath = Path.Combine(this._gs.TempFolderPath, filename);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

                sqlQuery = $"SELECT * FROM ( {sqlQuery} ) alias_{DateTime.Now.Ticks}";
                await using (DbDataReader reader = await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, CommandBehavior.SequentialAccess, token)) {
                    DbDataReader rdr = reader is IWrappedDataReader dapper
                        ? (DbDataReader)dapper.Reader
                        : reader;

                    await using (rdr) {
                        await rdr.ToCsv(delimiter, tempPath, includeHeader, useDoubleQuote, allUppercase, encoding ?? Encoding.UTF8, token);
                    }
                }

                string realPath = Path.Combine(outputFolderPath ?? this._gs.CsvFolderPath, filename);
                if (File.Exists(realPath)) {
                    File.Delete(realPath);
                }

                File.Move(tempPath, $"{realPath}.tmp", true);
                File.Move($"{realPath}.tmp", realPath, true);

                return realPath;
            }
            catch (Exception ex) {
                this._logger.LogError("[BULK_GET_CSV] {ex}", ex.Message);
                throw;
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        // Saran :: Kalau Ada Bawaan Library Mending Di Timpa Pakai Nativenya
        public virtual async Task<int> BulkInsertInto(string tableName, IDataReader dataReader, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            int result = 0;

            string[] columns = [.. Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName)];
            string sql = $@"
                INSERT INTO {tableName} ({string.Join(", ", columns)})
                VALUES ({string.Join(", ", columns.Select(c => "@" + c))})
            ";

            DbTransaction transaction = null;

            try {
                await this.OpenConnectionAsync(token);

                transaction = this.DbConnection.BeginTransaction();

                await using (DbCommand cmd = this.DbConnection.CreateCommand()) {
                    cmd.CommandText = sql;
                    cmd.Transaction = transaction;
                    cmd.CommandTimeout = commandTimeoutSeconds;

                    var dbParams = new DbParameter[columns.Length];
                    for (int i = 0; i < columns.Length; i++) {
                        DbParameter p = cmd.CreateParameter();
                        p.ParameterName = columns[i];
                        // p.Value = DBNull.Value;

                        _ = cmd.Parameters.Add(p);

                        dbParams[i] = p;
                    }

                    while (dataReader.Read()) {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < columns.Length; i++) {
                            dbParams[i].Value = dataReader.GetValue(i) ?? DBNull.Value;
                        }

                        _ = await cmd.ExecuteNonQueryAsync(token);

                        result++;
                    }

                    await transaction.CommitAsync(token);

                    return result;
                }
            }
            catch (Exception ex) {
                if (transaction != null) {
                    try {
                        await transaction.RollbackAsync(token);
                    }
                    catch (Exception rollEx) {
                        this._logger.LogError("[BULK_INSERT_ROLLBACK] {ex}", rollEx.Message);
                    }
                }

                this._logger.LogError("[BULK_INSERT_ERROR] {ex}", ex.Message);
                throw;
            }
            finally {
                await this.TryCloseConnectionAsync(token: token);
            }
        }

        // Saran :: Kalau Ada Bawaan Library Mending Di Timpa Pakai Nativenya
        public virtual Task<int> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            DataTableReader dr = dataTable.CreateDataReader();
            return this.BulkInsertInto(tableName, dr, commandTimeoutSeconds, chunkSize, token);
        }

        private async IAsyncEnumerable<int> BulkInsertInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string tableName, IEnumerable<T> dataList,
            Func<IEnumerator<T>, EnumeratorObjectDataReader<T>> readerFactory,
            int chunkSize = 2048, int commandTimeoutSeconds = 3600, [EnumeratorCancellation] CancellationToken token = default
        ) {
            using (IEnumerator<T> sharedEnumerator = dataList.GetEnumerator()) {
                bool hasMore = true;

                while (hasMore) {
                    token.ThrowIfCancellationRequested();

                    using (EnumeratorObjectDataReader<T> reader = readerFactory(sharedEnumerator)) {
                        int res = await this.BulkInsertInto(tableName, reader, commandTimeoutSeconds, chunkSize, token);

                        yield return res;

                        if (res < chunkSize) {
                            hasMore = false;
                        }
                    }
                }
            }
        }

        public virtual IAsyncEnumerable<int> BulkInsertIntoIAE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(JsonTypeInfo<T> jsonTypeInfo, string tableName, IEnumerable<T> dataListArray, int chunkSize = 2048, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new() {
            return this.BulkInsertInternal(
                tableName, dataListArray,
                e => new EnumeratorObjectDataReader<T>(e, jsonTypeInfo, chunkSize),
                chunkSize, commandTimeoutSeconds, token
            );
        }

        public virtual IAsyncEnumerable<int> BulkInsertIntoIAE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string tableName, IEnumerable<T> dataListArray, int chunkSize = 2048, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (ObjectExtension.IsSimpleType(t)) {
                throw new Exception("Hanya Diperbolehkan List Data Array Dari Object [Class/Record/Struct] Yang Mempuyai Key & Value Untuk Nama Kolom Dan Isi Datanya");
            }

            return this.BulkInsertInternal(
                tableName, dataListArray,
                e => new EnumeratorObjectDataReader<T>(e, chunkSize),
                chunkSize, commandTimeoutSeconds, token
            );
        }

        public virtual async Task<int> BulkInsertInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(JsonTypeInfo<T> jsonTypeInfo, string tableName, IEnumerable<T> dataListArray, int commandTimeoutSeconds = 3600, CancellationToken token = default) where T : JsonSerDe, new() {
            int result = 0;

            IAsyncEnumerable<int> iae = this.BulkInsertIntoIAE(
                jsonTypeInfo,
                tableName,
                dataListArray,
                commandTimeoutSeconds: commandTimeoutSeconds,
                token: token
            );

            await foreach (int i in iae.WithCancellation(token)) {
                result += i;
            }

            return result;
        }

        public virtual async Task<int> BulkInsertInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string tableName, IEnumerable<T> dataListArray, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            int result = 0;

            IAsyncEnumerable<int> iae = this.BulkInsertIntoIAE(
                tableName,
                dataListArray,
                commandTimeoutSeconds: commandTimeoutSeconds,
                token: token
            );

            await foreach (int i in iae.WithCancellation(token)) {
                result += i;
            }

            return result;
        }

    }

}