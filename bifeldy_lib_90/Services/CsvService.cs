using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using ChoETL;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Services {

    public interface ICsvService {
        List<CCsvColumn> GetColumnFromClassType<T>();
        List<CCsvColumn> GetColumnFromClassType<T>(JsonTypeInfo<T> jsonTypeInfo) where T : JsonSerDe, new();
        DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        IEnumerable<T> Csv2Enumerable<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        IEnumerable<T> Csv2Enumerable<T>(JsonTypeInfo<T> jsonTypeInfo, string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) where T : JsonSerDe, new();
    }

    public sealed class CCsvService : ICsvService {

        public CCsvService() {
            //
        }

        public List<CCsvColumn> GetColumnFromClassType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>() {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            PropertyInfo[] properties = typeof(T).GetProperties();

            var csvColumn = new List<CCsvColumn>();
            foreach (PropertyInfo prop in properties) {
                csvColumn.Add(new() { ColumnName = prop.Name, Position = 0, FieldType = prop.PropertyType, FieldName = prop.Name });
            }

            return csvColumn;
        }

        public List<CCsvColumn> GetColumnFromClassType<T>(JsonTypeInfo<T> jsonTypeInfo) where T : JsonSerDe, new() {
            var csvColumn = new List<CCsvColumn>();

            foreach (JsonPropertyInfo prop in jsonTypeInfo.Properties) {
                var csvColumnProp = new CCsvColumn() {
                    ColumnName = prop.Name,
                    Position = 0,
                    FieldType = prop.PropertyType,
                    FieldName = prop.Name
                };

                csvColumn.Add(csvColumnProp);
            }

            return csvColumn;
        }

        // Posisi Kolom CSV StartLibrary Dari 1 Bukan 0
        private ChoCSVReader<object> ChoEtlSetupCsv(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            if (string.IsNullOrEmpty(eolDelimiter)) {
                LineEndingType lineEnding = CsvLineEndingChecker.DetectLineEndings(filePath);
                switch (lineEnding) {
                    case LineEndingType.CRLF:
                        eolDelimiter = "\r\n";
                        break;
                    case LineEndingType.LF:
                        eolDelimiter = "\n";
                        break;
                    case LineEndingType.Mixed:
                        eolDelimiter = Environment.NewLine;
                        break;
                    default:
                        throw new Exception($"Tidak dapat mendeteksi jenis line ending pada file '{filePath}'.");
                }
            }

            var cfg = new ChoCSVRecordConfiguration() {
                Delimiter = delimiter,
                MayHaveQuotedFields = true,
                MayContainEOLInData = true,
                EOLDelimiter = eolDelimiter,
                NullValue = nullValue,
                QuoteAllFields = true,
                Encoding = encoding ?? Encoding.UTF8,
                DetectEncodingFromByteOrderMarks = encoding == null,
                MaxLineSize = 1_000_000
            };

            ChoCSVReader<object> csv = new ChoCSVReader(filePath, cfg);

            if (csvColumn != null) {
                csv = csv.WithFirstLineHeader(false);
                csvColumn = [.. csvColumn.OrderBy(c => c.Position)];

                foreach (CCsvColumn cc in csvColumn) {
                    csv = csv.WithField(cc.ColumnName, cc.Position, cc.FieldType, fieldName: cc.ColumnName);
                }
            }
            else {
                csv = csv.WithFirstLineHeader(true);
            }

            return csv;
        }

        public DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            var fi = new FileInfo(filePath);

            using (ChoCSVReader<object> csv = this.ChoEtlSetupCsv(fi.FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                DataTable dt = csv.AsDataTable(tableName ?? fi.Name);

                foreach (DataRow row in dt.Rows) {
                    foreach (DataColumn col in dt.Columns) {
                        if (row[col] is string s) {
                            if (s.Contains("\"\"")) {
                                row[col] = s.Replace("\"\"", "\"");
                            }
                        }
                    }
                }

                return dt;
            }
        }

        public string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            var sb = new StringBuilder();

            using (ChoCSVReader<object> csv = this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                IEnumerable<Dictionary<string, object>> cleaned = csv.Select(record => {
                    var dict = new Dictionary<string, object>();

                    foreach (KeyValuePair<string, object> kvp in (IDictionary<string, object>)record) {
                        dict[kvp.Key] = kvp.Value;
                        if (dict[kvp.Key] is string s) {
                            if (s.Contains("\"\"")) {
                                dict[kvp.Key] = s.Replace("\"\"", "\"");
                            }
                        }
                    }

                    return dict;
                });

                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(cleaned);
                }
            }

            return sb.ToString();
        }

        public IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            return this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8).AsDataReader();
        }

        public IEnumerable<T> Csv2Enumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            using (IDataReader dr = this.Csv2DataReader(filePath, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                var readerColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < dr.FieldCount; i++) {
                    readerColumns[dr.GetName(i)] = i;
                }

                var mappings = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && readerColumns.ContainsKey(p.Name))
                    .Select(p => new CsvMapping(
                        p.PropertyType,
                        ObjectExtension.CreateSetter(p),
                        readerColumns[p.Name]
                    ))
                    .ToList();

                while (dr.Read()) {
                    T objT = Activator.CreateInstance<T>();

                    foreach (CsvMapping map in mappings) {
                        if (!dr.IsDBNull(map.Index)) {
                            object val = dr.GetValue(map.Index);

                            if (val is string s && s.Contains("\"\"")) {
                                val = s.Replace("\"\"", "\"");
                            }

                            val = Convert.ChangeType(val, map.PropType);
                            map.Setter(objT, val);
                        }
                    }

                    yield return objT;
                }
            }
        }

        public IEnumerable<T> Csv2Enumerable<T>(JsonTypeInfo<T> jsonTypeInfo, string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) where T : JsonSerDe, new() {
            using (IDataReader dr = this.Csv2DataReader(filePath, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                var mappings = new List<(JsonPropertyInfo Prop, int Index, Type TargetType)>();

                var readerColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < dr.FieldCount; i++) {
                    readerColumns[dr.GetName(i)] = i;
                }

                foreach (JsonPropertyInfo prop in jsonTypeInfo.Properties) {
                    if (prop.Set == null) {
                        continue; // Skip read-only
                    }

                    if (readerColumns.TryGetValue(prop.Name, out int index)) {
                        Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        mappings.Add((prop, index, targetType));
                    }
                }

                while (dr.Read()) {
                    if (jsonTypeInfo.CreateObject == null) {
                        throw new InvalidOperationException($"Type {typeof(T).Name} must have a parameterless constructor.");
                    }

                    T objT = jsonTypeInfo.CreateObject();

                    foreach ((JsonPropertyInfo Prop, int Index, Type TargetType) map in mappings) {
                        if (!dr.IsDBNull(map.Index)) {
                            object val = dr.GetValue(map.Index);

                            if (val is string s && s.Contains("\"\"")) {
                                val = s.Replace("\"\"", "\"");
                            }

                            val = Convert.ChangeType(val, map.TargetType);
                            map.Prop.Set(objT, val);
                        }
                    }

                    yield return objT;
                }
            }
        }

        private record CsvMapping(Type PropType, Action<object, object> Setter, int Index);

    }

}