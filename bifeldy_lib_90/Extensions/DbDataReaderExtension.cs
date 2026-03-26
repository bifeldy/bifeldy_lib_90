using bifeldy_lib_90.Abstractions;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using static bifeldy_lib_90.Libraries.ToCsv;

namespace bifeldy_lib_90.Extensions {

    public static class DbDataReaderExtension {

        public static object ReadValue(DbDataReader dr, int index, Type targetType) {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (targetType.IsEnum) {
                Type enumBase = Enum.GetUnderlyingType(targetType);
                object raw = ReadValue(dr, index, enumBase);

                if (!Enum.IsDefined(targetType, raw)) {
                    throw new InvalidCastException($"Value {raw} is not defined in enum {targetType.Name}");
                }

                return Enum.ToObject(targetType, raw);
            }

            return targetType switch {
                var t when t == typeof(string) => dr.GetString(index),
                var t when t == typeof(int) => dr.GetInt32(index),
                var t when t == typeof(long) => dr.GetInt64(index),
                var t when t == typeof(bool) => dr.GetBoolean(index),
                var t when t == typeof(decimal) => dr.GetDecimal(index),
                var t when t == typeof(double) => dr.GetDouble(index),
                var t when t == typeof(float) => dr.GetFloat(index),
                var t when t == typeof(DateTime) => dr.GetDateTime(index),
                var t when t == typeof(DateTimeOffset) => dr.GetFieldValue<DateTimeOffset>(index),
                var t when t == typeof(Guid) => dr.GetGuid(index),
                var t when t == typeof(byte[]) => (byte[])dr.GetValue(index),
                _ => dr.GetValue(index)
            };
        }

        private static async IAsyncEnumerable<T> ToAsyncInternal<T>(
            DbDataReader dr,
            List<DataReaderMapping> mappings,
            Func<T> factory,
            Action<T> callback,
            [EnumeratorCancellation] CancellationToken token
        ) {
            while (await dr.ReadAsync(token)) {
                T obj = factory();

                foreach (DataReaderMapping m in mappings) {
                    if (!await dr.IsDBNullAsync(m.Index, token)) {
                        object value = ReadValue(dr, m.Index, m.TargetType);
                        m.Setter(obj, value);
                    }
                }

                callback?.Invoke(obj);
                yield return obj;
            }
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            this DbDataReader dr,
            Action<T> callback = null,
            [EnumeratorCancellation] CancellationToken token = default
        ) {
            if (dr == null) {
                yield break;
            }

            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (ObjectExtension.IsSimpleType(t)) {
                while (await dr.ReadAsync(token)) {
                    T objT = default;

                    if (!await dr.IsDBNullAsync(0, token)) {
                        object val = dr.GetValue(0);
                        objT = (T)Convert.ChangeType(val, typeof(T));
                    }

                    callback?.Invoke(objT);
                    yield return objT;
                }

                yield break;
            }

            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var colIndexLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dr.FieldCount; i++) {
                colIndexLookup[dr.GetName(i)] = i;
            }

            var mappings = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && colIndexLookup.ContainsKey(p.Name))
                .Select(p => new DataReaderMapping(
                    p.PropertyType,
                    ObjectExtension.CreateSetter(p),
                    colIndexLookup[p.Name]
                ))
                .ToList();

            IAsyncEnumerable<T> iae = ToAsyncInternal(dr, mappings, Activator.CreateInstance<T>, callback, token);
            await foreach (T item in iae.WithCancellation(token)) {
                yield return item;
            }
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this DbDataReader dr,
            JsonTypeInfo<T> jsonTypeInfo,
            Action<T> callback = null,
            [EnumeratorCancellation] CancellationToken token = default
        ) where T : JsonSerDe, new() {
            if (dr == null) {
                yield break;
            }

            var colIndexLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dr.FieldCount; i++) {
                colIndexLookup[dr.GetName(i)] = i;
            }

            var mappings = jsonTypeInfo.Properties
                .Where(p => p.Set != null && colIndexLookup.ContainsKey(p.Name))
                .Select(p => new DataReaderMapping(
                    p.PropertyType,
                    (obj, val) => p.Set(obj, val),
                    colIndexLookup[p.Name]
                ))
                .ToList();

            Func<T> factory = jsonTypeInfo.CreateObject != null
                ? () => jsonTypeInfo.CreateObject()
                : () => new T();

            IAsyncEnumerable<T> iae = ToAsyncInternal(dr, mappings, factory, callback, token);
            await foreach (T item in iae.WithCancellation(token)) {
                yield return item;
            }
        }

        public static async Task ToCsv(this DbDataReader dr, string delimiter, string outputFilePath, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            await using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8, 65536)) {
                int fieldCount = dr.FieldCount;
                var sb = new StringBuilder();

                if (includeHeader) {
                    for (int i = 0; i < fieldCount; i++) {
                        _ = sb.Append(CheckHeaderLineCsv(dr.GetName(i), useDoubleQuote, allUppercase));
                        if (i < fieldCount - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }

                while (await dr.ReadAsync(token)) {
                    for (int i = 0; i < fieldCount; i++) {
                        object val = await dr.IsDBNullAsync(i, token) ? null : dr.GetValue(i);
                        _ = sb.Append(CheckRowLineCsv(val, delimiter, useDoubleQuote, allUppercase));
                        if (i < fieldCount - 1) {
                            _ = sb.Append(delimiter);
                        }
                    }

                    await streamWriter.WriteLineAsync(sb, token);
                    _ = sb.Clear();
                }
            }
        }

        public record DataReaderMapping(Type TargetType, Action<object, object> Setter, int Index);

    }

}