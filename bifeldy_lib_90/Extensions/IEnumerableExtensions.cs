using bifeldy_lib_90.Abstractions;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class IEnumerableExtension {

        public static DataTable ToDataTable<T>(this IEnumerable<T> items, JsonTypeInfo<T> jsonTypeInfo, string tableName) where T : JsonSerDe {
            if (string.IsNullOrEmpty(tableName)) {
                tableName = typeof(T).Name;
            }

            var dt = new DataTable(tableName);
            bool columnsInitialized = false;

            foreach (T item in items) {
                JsonObject node = JsonSerializer.SerializeToNode(item, jsonTypeInfo)?.AsObject();
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

        public static async Task ToCsv<T>(this IEnumerable<T> arrayListData, JsonTypeInfo<T> jsonTypeInfo, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) where T : JsonSerDe {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8)) {
                IList<JsonPropertyInfo> properties = jsonTypeInfo.Properties;

                if (includeHeader) {
                    string headerLine = string.Join(delimiter, properties.Select(prop => {
                        string name = prop.Name;

                        if (allUppercase) {
                            name = name.ToUpper();
                        }

                        if (useDoubleQuote) {
                            name = $"\"{name.Replace("\"", "\"\"")}\"";
                        }

                        return name;
                    }));

                    await streamWriter.WriteLineAsync(headerLine.AsMemory(), token);
                }

                foreach (T item in arrayListData) {
                    string line = string.Join(delimiter, properties.Select(prop => {
                        object value = prop.Get(item);
                        if (value == null) {
                            return "";
                        }

                        string text = value.ToString();
                        if (value is DateTime dt) {
                            text = dt.ToString("O");
                        }

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        bool mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
                        if (useDoubleQuote || mustQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteLineAsync(line.AsMemory(), token);
                }
            }
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> items) {
            foreach (T item in items) {
                yield return item;
                await Task.Yield();
            }
        }

    }

}
