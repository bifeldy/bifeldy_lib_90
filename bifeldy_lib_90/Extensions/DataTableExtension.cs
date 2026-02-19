using bifeldy_lib_90.Abstractions;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using static bifeldy_lib_90.Libraries.ToCsv;

namespace bifeldy_lib_90.Extensions {

    public static class DataTableExtensions {

        private static IEnumerable<T> ToEnumerableInternal<T>(
            DataTable dt,
            List<DataTableMapping> mappings,
            Func<T> factory,
            Action<T> callback
        ) {
            foreach (DataRow row in dt.Rows) {
                T objT = factory();

                foreach (DataTableMapping map in mappings) {
                    object val = row[map.ColumnIndex];

                    if (val != DBNull.Value && val != null) {
                        if (val.GetType() != map.TargetType) {
                            val = FastConvert(val, map.TargetType);
                        }

                        map.Setter(objT, val);
                    }
                }

                callback?.Invoke(objT);
                yield return objT;
            }
        }

        public static IEnumerable<T> ToEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this DataTable dt, Action<T> callback = null) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            if (dt == null) {
                yield break;
            }

            dt.CaseSensitive = false;

            var mappings = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && dt.Columns.Contains(p.Name))
                .Select(p => new DataTableMapping(
                    p.PropertyType,
                    (obj, val) => p.SetValue(obj, val),
                    dt.Columns.IndexOf(p.Name)
                ))
                .ToList();

            foreach (T item in ToEnumerableInternal(dt, mappings, Activator.CreateInstance<T>, callback)) {
                yield return item;
            }
        }

        public static IEnumerable<T> ToEnumerable<T>(this DataTable dt, JsonTypeInfo<T> jsonTypeInfo, Action<T> callback = null) where T : JsonSerDe, new() {
            if (dt == null) {
                yield break;
            }

            dt.CaseSensitive = false;

            var mappings = jsonTypeInfo.Properties
                .Where(p => p.Set != null && dt.Columns.Contains(p.Name))
                .Select(p => new DataTableMapping(
                    p.PropertyType,
                    (obj, val) => p.Set(obj, val),
                    dt.Columns.IndexOf(p.Name)
                ))
                .ToList();

            Func<T> factory = jsonTypeInfo.CreateObject != null
                ? () => jsonTypeInfo.CreateObject()
                : () => new T();

            foreach (T item in ToEnumerableInternal(dt, mappings, factory, callback)) {
                yield return item;
            }
        }

        public static async Task ToCsv(this DataTable dt, string delimiter, string outputFilePath, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            await using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8, 65536)) {
                var sb = new StringBuilder();
                int colCount = dt.Columns.Count;

                if (includeHeader) {
                    for (int i = 0; i < colCount; i++) {
                        _ = sb.Append(CheckHeaderLineCsv(dt.Columns[i].ColumnName, useDoubleQuote, allUppercase));
                        if (i < colCount - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }

                foreach (DataRow row in dt.Rows) {
                    for (int i = 0; i < colCount; i++) {
                        object value = row[i] == DBNull.Value ? null : row[i];
                        _ = sb.Append(CheckRowLineCsv(value, delimiter, useDoubleQuote, allUppercase));
                        if (i < colCount - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }
            }
        }

        private static object FastConvert(object value, Type targetType) {
            Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try {
                if (actualType == typeof(string)) {
                    return value.ToString();
                }

                if (actualType == typeof(Guid)) {
                    return Guid.Parse(value.ToString());
                }

                return Convert.ChangeType(value, actualType);
            }
            catch {
                return null;
            }
        }

        private record DataTableMapping(Type TargetType, Action<object, object> Setter, int ColumnIndex);

    }

}