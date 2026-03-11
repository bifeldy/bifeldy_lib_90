using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text;

namespace bifeldy_lib_90.Databases {

    public interface IPostgres : IDatabase {
        CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName);
        CPostgres CloneConnection();
    }

    public sealed class CPostgres : CDatabase, IPostgres {

        private readonly IServerConfigRepository _scr;

        private string DbIpAddrss { get; set; }
        private string DbPort { get; set; }
        private string DbName { get; set; }
        private string DbUsername { get; set; }
        private string DbPassword { get; set; }

        public CPostgres(
            IOptions<EnvVar> envVar,
            ILogger<CPostgres> logger,
            IApplicationService @as,
            IGlobalService gs,
            IHttpContextAccessor hca,
            IServerConfigRepository scr
        ) : base(envVar, logger, @as, gs, hca) {
            this._scr = scr;
            this.InitializeConnection();
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbPort = null, string dbUsername = null, string dbPassword = null, string dbName = null) {
            string kunciGxxx = null;

            if (this._hca.HttpContext != null) {
                kunciGxxx = this._hca.HttpContext.Items["kunci_gxxx"]?.ToString();
            }

            kunciGxxx ??= this._scr.CurrentLoadedKodeServerKunciDc();

            this.DbIpAddrss = dbIpAddrss ?? this._as.GetVariabel("IPPostgres", kunciGxxx);
            this.DbPort = dbPort ?? this._as.GetVariabel("PortPostgres", kunciGxxx);
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserPostgres", kunciGxxx);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordPostgres", kunciGxxx);
            this.DbName = dbName ?? this._as.GetVariabel("DatabasePostgres", kunciGxxx);

            string _dbConnectionString = $"Host={this.DbIpAddrss};Port={this.DbPort};Username={this.DbUsername};Password={this.DbPassword};Database={this.DbName};Timeout=180;ApplicationName={this._as.AppName}_{this._as.AppVersion};"; // 3 Minutes
            this.DbConnection = new NpgsqlConnection(_dbConnectionString);
        }

        public override async Task<string> BulkGetCsv(string sqlQuery, string delimiter, string filename, string outputFolderPath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, DynamicParameters sqlParameter = null, int commandTimeoutSeconds = 3600, Encoding encoding = null, CancellationToken token = default) {
            string result = null;
            Exception exception = null;

            try {
                encoding ??= Encoding.UTF8;

                // if (sqlParameter != null) {
                //     return base.BulkGetCsv(sqlQuery, delimiter, filename, outputFolderPath, includeHeader, useDoubleQuote, allUppercase, sqlParameter, commandTimeoutSeconds, encoding, token);
                // }

                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, filename);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

                if (includeHeader) {
                    sqlQuery = $"SELECT * FROM ( {sqlQuery} ) alias_{DateTime.Now.Ticks} WHERE 1 = 0";

                    await using (var rdr = (NpgsqlDataReader)await this.ExecReaderAsync(sqlQuery, sqlParameter, commandTimeoutSeconds, CommandBehavior.SequentialAccess, token)) {
                        ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();
                        string header = string.Join(delimiter, columns.Select(c => {
                            string text = c.ColumnName;

                            if (allUppercase) {
                                text = text.ToUpper();
                            }

                            if (useDoubleQuote) {
                                text = $"\"{text.Replace("\"", "\"\"")}\"";
                            }

                            return text;
                        }));

                        await using (var writer = new StreamWriter(tempPath, false, encoding)) {
                            await writer.WriteLineAsync(header.AsMemory(), token);
                        }
                    }
                }

                sqlQuery = $"COPY ({sqlQuery}) TO STDOUT WITH CSV DELIMITER '{delimiter}'";
                if (!useDoubleQuote) {
                    sqlQuery += " QUOTE '\x01'";
                }

                using (TextReader reader = await ((NpgsqlConnection)this.DbConnection).BeginTextExportAsync(sqlQuery, token)) {
                    await using (var writer = new StreamWriter(tempPath, true, encoding)) {
                        string line = string.Empty;
                        while ((line = await reader.ReadLineAsync(token)) != null && !token.IsCancellationRequested) {
                            if (allUppercase) {
                                line = line.ToUpper();
                            }

                            if (!useDoubleQuote) {
                                if (line.Contains("\x01")) {
                                    line = line.Replace("\x01", "");
                                }
                            }

                            await writer.WriteLineAsync(line.AsMemory(), token);
                        }
                    }
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
                this._logger.LogError("[PG_BULK_GET_CSV] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                this.TryCloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        private async Task<(NpgsqlDbType[] types, int[] lengths, string[] fieldNames)> GetMetadata(
            string tableName,
            string[] columnNames,
            int commandTimeoutSeconds = 3600,
            CancellationToken token = default
        ) {
            string sqlQuery = $"SELECT * FROM {tableName} WHERE 1 = 0";

            await using (DbDataReader reader = await this.ExecReaderAsync(sqlQuery, null, commandTimeoutSeconds, CommandBehavior.SchemaOnly, token)) {
                NpgsqlDataReader rdr = reader is IWrappedDataReader dapper
                    ? (NpgsqlDataReader)dapper.Reader
                    : (NpgsqlDataReader)reader;

                await using (rdr) {
                    ReadOnlyCollection<NpgsqlDbColumn> dbColumns = rdr.GetColumnSchema();
                    int colCount = columnNames.Length;

                    var types = new NpgsqlDbType[colCount];
                    int[] lengths = new int[colCount];
                    string[] fieldNames = new string[colCount];

                    for (int i = 0; i < colCount; i++) {
                        NpgsqlDbColumn column = dbColumns.FirstOrDefault(c => c.ColumnName.Equals(columnNames[i], StringComparison.OrdinalIgnoreCase));
                        if (column == null) {
                            throw new Exception($"Kolom {columnNames[i]} tidak ditemukan di {tableName}");
                        }

                        types[i] = (NpgsqlDbType)column.NpgsqlDbType;
                        lengths[i] = column.ColumnSize ?? 0;
                        fieldNames[i] = column.ColumnName;
                    }

                    return (types, lengths, fieldNames);
                }
            }
        }

        private async Task BulkInsertData(NpgsqlBinaryImporter writer, object obj, NpgsqlDbType types, int lengths, CancellationToken token) {
            switch (types) {
                case NpgsqlDbType.Bigint:
                    await writer.WriteAsync(Convert.ToInt64(obj), types, token);
                    break;
                case NpgsqlDbType.Integer:
                    await writer.WriteAsync(Convert.ToInt32(obj), types, token);
                    break;
                case NpgsqlDbType.Smallint:
                    await writer.WriteAsync(Convert.ToInt16(obj), types, token);
                    break;
                case NpgsqlDbType.Money:
                case NpgsqlDbType.Numeric:
                    await writer.WriteAsync(Convert.ToDecimal(obj).RemoveTrail(), types, token);
                    break;
                case NpgsqlDbType.Double:
                    await writer.WriteAsync(Convert.ToDouble(obj), types, token);
                    break;
                case NpgsqlDbType.Real:
                    await writer.WriteAsync(Convert.ToSingle(obj), types, token);
                    break;
                case NpgsqlDbType.Boolean:
                    await writer.WriteAsync(Convert.ToBoolean(obj), types, token);
                    break;
                case NpgsqlDbType.Char:
                    if (lengths == 1) {
                        string str = Convert.ToString(obj);
                        if (string.IsNullOrEmpty(str)) {
                            obj = string.Empty;
                        }
                        else {
                            char[] chr = str.ToCharArray();
                            if (chr.Length == lengths) {
                                await writer.WriteAsync(chr[lengths - 1], types, token);
                                break;
                            }
                        }
                    }

                    goto case NpgsqlDbType.Varchar;
                case NpgsqlDbType.Varchar:
                case NpgsqlDbType.Text:
                    await writer.WriteAsync(Convert.ToString(obj), types, token);
                    break;
                case NpgsqlDbType.Time:
                case NpgsqlDbType.Timestamp:
                case NpgsqlDbType.TimestampTz:
                case NpgsqlDbType.Date:
                    await writer.WriteAsync(Convert.ToDateTime(obj), types, token);
                    break;
                case NpgsqlDbType.Bytea:
                    await writer.WriteAsync((byte[])obj, types, token);
                    break;
                default:
                    await writer.WriteAsync(obj, types, token);
                    break;

                    //
                    // TODO :: Add More Handles While Free Time ~
                    //
            }
        }

        public override async Task<int> BulkInsertInto(string tableName, IDataReader dataReader, int commandTimeoutSeconds = 3600, int chunkSize = 2048, CancellationToken token = default) {
            try {
                int result = 0;

                if (string.IsNullOrEmpty(tableName)) {
                    throw new Exception("Target Tabel Tidak Ditemukan");
                }

                string[] readerColumns = [.. Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName)];

                (NpgsqlDbType[] types, int[] lengths, string[] fieldNames) = await this.GetMetadata(tableName, readerColumns, commandTimeoutSeconds, token);

                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < readerColumns.Length; p++) {
                    _ = sB.Append(", " + fieldNames[p]);
                }

                await using (NpgsqlBinaryImporter writer = await ((NpgsqlConnection)this.DbConnection).BeginBinaryImportAsync($"COPY {tableName} ({sB}) FROM STDIN (FORMAT BINARY)", token)) {
                    while (dataReader.Read()) {
                        await writer.StartRowAsync(token);

                        for (int i = 0; i < fieldNames.Length; i++) {
                            int colIndex = dataReader.GetOrdinal(fieldNames[i]);
                            object _obj = dataReader.GetValue(colIndex);

                            if (_obj == null || _obj == DBNull.Value) {
                                await writer.WriteNullAsync(token);
                            }
                            else {
                                await this.BulkInsertData(writer, _obj, types[i], lengths[i], token);
                            }
                        }

                        result++;
                    }

                    _ = await writer.CompleteAsync(token);
                }

                return result;
            }
            catch (Exception ex) {
                this._logger.LogError("[PG_BULK_INSERT_ERROR] {ex}", ex.Message);
                throw;
            }
            finally {
                this.TryCloseConnection();
            }
        }

        public CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName) {
            var postgres = (CPostgres)this.Clone();
            postgres.InitializeConnection(dbIpAddrss, dbPort, dbUsername, dbPassword, dbName);
            return postgres;
        }

        public CPostgres CloneConnection() {
            var postgres = (CPostgres)this.Clone();
            postgres.InitializeConnection(this.DbIpAddrss, this.DbPort, this.DbUsername, this.DbPassword, this.DbName);
            return postgres;
        }

    }

}