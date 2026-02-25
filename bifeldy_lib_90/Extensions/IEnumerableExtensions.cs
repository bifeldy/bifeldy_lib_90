using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using static bifeldy_lib_90.Libraries.ToCsv;

namespace bifeldy_lib_90.Extensions {

    public static class IEnumerableExtension {

        private static DataTable ToDataTableInternal<T>(
            this IEnumerable<T> items,
            Func<T, JsonNode> callbackT,
            string tableName
        ) {
            if (string.IsNullOrEmpty(tableName)) {
                tableName = typeof(T).Name;
            }

            var dt = new DataTable(tableName);
            bool columnsInitialized = false;

            foreach (T item in items) {
                JsonObject node = callbackT(item)?.AsObject();
                if (node == null) {
                    continue;
                }

                if (!columnsInitialized) {
                    foreach (KeyValuePair<string, JsonNode> kv in node) {
                        _ = dt.Columns.Add(kv.Key, typeof(object));
                    }

                    columnsInitialized = true;
                }

                DataRow row = dt.NewRow();
                foreach (KeyValuePair<string, JsonNode> kv in node) {
                    row[kv.Key] = (object)kv.Value?.ToString() ?? DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            dt.CaseSensitive = false;

            return dt;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safety guaranteed by JsonTypeInfo usage.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode", Justification = "We are explicitly registering types to ensure AOT generation.")]
        public static DataTable ToDataTable<T>(this IEnumerable<T> items, string tableName = null) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new DecimalConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalConverter());

            return items.ToDataTableInternal(
                (t) => JsonSerializer.SerializeToNode(t, jsonSerializerOptions),
                tableName
            );
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> items, JsonTypeInfo<T> jsonTypeInfo, string tableName = null) where T : JsonSerDe, new() {
            return items.ToDataTableInternal(
                (t) => JsonSerializer.SerializeToNode(t, jsonTypeInfo),
                tableName
            );
        }

        private static async Task WriteCsvInternal<T>(
            IEnumerable<T> data,
            IEnumerable<CsvColumnMapping> columns,
            string delimiter,
            string path,
            bool includeHeader,
            bool useDoubleQuote,
            bool allUppercase,
            Encoding encoding,
            CancellationToken token
        ) {
            var cols = columns.ToList();

            await using (var streamWriter = new StreamWriter(path, false, encoding ?? Encoding.UTF8, 65536)) {
                var sb = new StringBuilder();

                if (includeHeader) {
                    for (int i = 0; i < cols.Count; i++) {
                        _ = sb.Append(CheckHeaderLineCsv(cols[i].Name, useDoubleQuote, allUppercase));
                        if (i < cols.Count - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }

                foreach (T item in data) {
                    for (int i = 0; i < cols.Count; i++) {
                        object value = cols[i].GetValue(item);
                        _ = sb.Append(CheckRowLineCsv(value, delimiter, useDoubleQuote, allUppercase));
                        if (i < cols.Count - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }
            }
        }

        public static Task ToCsv<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IEnumerable<T> arrayListData, string delimiter, string outputFilePath, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            PropertyInfo[] properties = typeof(T).GetProperties();

            return WriteCsvInternal(
                arrayListData,
                properties.Select(p => new CsvColumnMapping(p.Name, obj => p.GetValue(obj))),
                delimiter, outputFilePath, includeHeader, useDoubleQuote, allUppercase, encoding, token
            );
        }

        public static Task ToCsv<T>(this IEnumerable<T> arrayListData, JsonTypeInfo<T> jsonTypeInfo, string delimiter, string outputFilePath, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) where T : JsonSerDe, new() {
            return WriteCsvInternal(
                arrayListData,
                jsonTypeInfo.Properties.Where(p => p.Get != null).Select(p => new CsvColumnMapping(p.Name, obj => p.Get(obj))),
                delimiter, outputFilePath, includeHeader, useDoubleQuote, allUppercase, encoding, token
            );
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> items, Func<T, Task<T>> modifier = null) {
            foreach (T item in items) {
                T i = item;

                if (modifier != null) {
                    i = await modifier(item);
                }

                yield return i;
            }
        }

    }

}