using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class DbDataReaderExtension {

        public static object ReadValue(DbDataReader dr, int index, Type targetType) {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (targetType == typeof(string)) {
                return dr.GetString(index);
            }

            if (targetType == typeof(int)) {
                return dr.GetInt32(index);
            }

            if (targetType == typeof(long)) {
                return dr.GetInt64(index);
            }

            if (targetType == typeof(bool)) {
                return dr.GetBoolean(index);
            }

            if (targetType == typeof(decimal)) {
                return dr.GetDecimal(index);
            }

            if (targetType == typeof(double)) {
                return dr.GetDouble(index);
            }

            if (targetType == typeof(float)) {
                return dr.GetFloat(index);
            }

            if (targetType == typeof(DateTime)) {
                return dr.GetDateTime(index);
            }

            if (targetType == typeof(DateTimeOffset)) {
                return dr.GetFieldValue<DateTimeOffset>(index);
            }

            if (targetType == typeof(Guid)) {
                return dr.GetGuid(index);
            }

            if (targetType == typeof(byte[])) {
                return (byte[])dr.GetValue(index);
            }

            if (targetType.IsEnum) {
                Type enumBase = Enum.GetUnderlyingType(targetType);
                object raw = ReadValue(dr, index, enumBase);
                return Enum.ToObject(targetType, raw);
            }

            return dr.GetValue(index);
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this DbDataReader dr, JsonTypeInfo<T> jsonTypeInfo, Action<T> callback = null, [EnumeratorCancellation] CancellationToken token = default) where T : JsonSerDe, new() {
            if (dr == null) {
                yield break;
            }

            var colIndexLookup = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < dr.FieldCount; i++) {
                colIndexLookup[dr.GetName(i)] = i;
            }

            var maps = new List<JsonKeyMap>(jsonTypeInfo.Properties.Count);

            foreach (JsonPropertyInfo p in jsonTypeInfo.Properties) {
                if (colIndexLookup.TryGetValue(p.Name, out int idx)) {
                    maps.Add(new JsonKeyMap(p, idx));
                }
            }

            JsonKeyMap[] mappings = [.. maps];

            while (await dr.ReadAsync(token)) {
                var obj = new T();

                foreach (JsonKeyMap m in mappings) {
                    if (!await dr.IsDBNullAsync(m.Index, token)) {
                        object value = ReadValue(dr, m.Index, m.Property.PropertyType);
                        m.Property.Set(obj, value);
                    }
                }

                callback?.Invoke(obj);
                yield return obj;
            }
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this DbDataReader dr, Action<T> callback = null, [EnumeratorCancellation] CancellationToken token = default) {
            if (dr == null) {
                yield break;
            }

            while (await dr.ReadAsync(token)) {
                T objT = default;

                Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                if (!ObjectExtension.IsSimpleType(t)) {
                    throw new Exception("Only `string` or ValueType allowed");
                }

                if (!await dr.IsDBNullAsync(0, token)) {
                    object val = dr.GetValue(0);
                    objT = (T)Convert.ChangeType(val, typeof(T));
                }

                callback?.Invoke(objT);
                yield return objT;
            }
        }

        public static async Task ToCsv(this DbDataReader dr, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8)) {
                if (includeHeader) {
                    string header = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        string text = dr.GetName(i);

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        if (useDoubleQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteLineAsync(header.AsMemory(), token);
                }

                while (await dr.ReadAsync(token)) {
                    string line = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        if (dr.IsDBNull(i)) {
                            return "";
                        }

                        object value = dr.GetValue(i);
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

    }

}
