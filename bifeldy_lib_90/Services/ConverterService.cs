using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Libraries;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace bifeldy_lib_90.Services {

    //
    // .NET 8/9 Roslyn
    //
    // Tipe Data `List<T>` Ga Bisa Pakai `JsonTypeInfo<T>`
    // Terpaksa Di Comment, Jadi Ga Ada Constraint
    // `where T : JsonSerDe, new()`
    //

    public interface IConverterService {
        byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument);
        T JsonToObject<T>(string json);
        T JsonToObject<T>(string json, JsonTypeInfo<T> typeInfo) /* where T : JsonSerDe, new() */;
        string ObjectToJson<T>(T value);
        string ObjectToJson<T>(T value, JsonTypeInfo<T> typeInfo) /* where T : JsonSerDe, new() */;
        string XmlToJson(string xml);
        object JsonNodeToObject(JsonNode node);
        JsonNode ObjectToJsonNode(object value);
        object JsonToObject(string json);
        string FormatByteSizeHumanReadable(long bytes, string forceUnit = null);
        List<CDynamicClassProperty> GetTableClassStructureModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>();
        List<CDynamicClassProperty> GetTableClassStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo) /* where T : JsonSerDe, new() */;
        List<CDynamicClassPropertyV2> GetPocoStructureModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>();
        List<CDynamicClassPropertyV2> GetPocoStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo) /* where T : JsonSerDe, new() */;
    }

    public sealed class CConverterService : IConverterService {

        private readonly IConverter _converter;

        private static JsonSerializerOptions JSON_SERIALIZER_OPTIONS = null;
        private static readonly object _lockObj = new();

        public CConverterService(IConverter converter) {
            this._converter = converter;
            //
            lock (_lockObj) {
                if (JSON_SERIALIZER_OPTIONS == null) {
                    JSON_SERIALIZER_OPTIONS = new JsonSerializerOptions();
                    JSON_SERIALIZER_OPTIONS.Converters.Add(new DecimalConverter());
                    JSON_SERIALIZER_OPTIONS.Converters.Add(new NullableDecimalConverter());
                }
            }
        }

        // Unfortunately the main wkhtmltopdf project is not active any more.
        // https://github.com/HakanL/WkHtmlToPdf-DotNet/issues/132
        public byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            return this._converter.Convert(htmlToPdfDocument);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safety guaranteed by JsonTypeInfo usage.")]
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Safe primitive types from JsonTypeInfo.")]
        public T JsonToObject<T>(string json) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            if (string.IsNullOrEmpty(json)) {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, JSON_SERIALIZER_OPTIONS);
        }

        public T JsonToObject<T>(string json, JsonTypeInfo<T> typeInfo) /* where T : JsonSerDe, new() */ {
            if (string.IsNullOrEmpty(json)) {
                return default;
            }

            return JsonSerializer.Deserialize(json, typeInfo);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safety guaranteed by JsonTypeInfo usage.")]
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Safe primitive types from JsonTypeInfo.")]
        public string ObjectToJson<T>(T value) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                return this.ObjectToJsonNode(value)?.ToJsonString(JSON_SERIALIZER_OPTIONS);
            }

            return value == null ? null : JsonSerializer.Serialize(value, JSON_SERIALIZER_OPTIONS);
        }

        public string ObjectToJson<T>(T value, JsonTypeInfo<T> typeInfo) /* where T : JsonSerDe, new() */ {
            if (value == null) {
                return null;
            }

            return JsonSerializer.Serialize(value, typeInfo);
        }

        private static JsonNode ConvertXmlToJsonNode(XElement element) {
            var jsonObject = new JsonObject();

            foreach (XAttribute attr in element.Attributes()) {
                jsonObject.Add($"@{attr.Name.LocalName}", attr.Value);
            }

            IEnumerable<IGrouping<string, XElement>> elementGroups = element.Elements().GroupBy(e => e.Name.LocalName);
            foreach (IGrouping<string, XElement> group in elementGroups) {
                if (group.Count() == 1) {
                    XElement child = group.First();
                    if (!child.HasElements && string.IsNullOrEmpty(child.Value)) {
                        jsonObject.Add(group.Key, null);
                    }
                    else {
                        jsonObject.Add(group.Key, ConvertXmlToJsonNode(child));
                    }
                }
                else {
                    var jsonArray = new JsonArray();
                    foreach (XElement child in group) {
                        jsonArray.Add(ConvertXmlToJsonNode(child));
                    }

                    jsonObject.Add(group.Key, jsonArray);
                }
            }

            if (!element.HasElements && element.Attributes().Any()) {
                if (!string.IsNullOrEmpty(element.Value)) {
                    jsonObject.Add("#value", element.Value);
                }
            }
            else if (!element.HasElements && !element.Attributes().Any()) {
                if (jsonObject.Count == 0) {
                    return JsonValue.Create(element.Value);
                }
            }

            return jsonObject;
        }

        public string XmlToJson(string xml) {
            if (string.IsNullOrEmpty(xml)) {
                return null;
            }

            var xdoc = XDocument.Parse(xml);
            xdoc.Declaration = null;

            if (xdoc == null || xdoc.Root == null) {
                return null;
            }

            JsonNode jsonNode = ConvertXmlToJsonNode(xdoc.Root);
            return jsonNode.ToJsonString();
        }

        public object JsonNodeToObject(JsonNode node) {
            if (node == null) {
                return null;
            }

            if (node is JsonObject obj) {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, JsonNode> kv in obj) {
                    dict[kv.Key] = kv.Value == null ? null : this.JsonNodeToObject(kv.Value);
                }

                return dict;
            }

            if (node is JsonArray arr) {
                var list = new List<object>();
                foreach (JsonNode item in arr) {
                    list.Add(item == null ? null : this.JsonNodeToObject(item));
                }

                return list;
            }

            if (node is JsonValue val) {
                if (val.TryGetValue(out string s)) {
                    return s;
                }

                if (val.TryGetValue(out bool b)) {
                    return b;
                }

                if (val.TryGetValue(out int i)) {
                    return i;
                }

                if (val.TryGetValue(out long l)) {
                    return l;
                }

                if (val.TryGetValue(out float f)) {
                    return f;
                }

                if (val.TryGetValue(out double d)) {
                    return d;
                }

                if (val.TryGetValue(out decimal m)) {
                    return m;
                }

                if (val.TryGetValue(out DateTime dt)) {
                    return dt;
                }

                if (val.TryGetValue(out DateOnly dto)) {
                    return dto;
                }

                //
                // TODO :: Add More Known Data Type
                //
                // ~ Note ~
                // Guid = String
                //

                throw new NotSupportedException("Unsupported JSON Primitive");
            }

            return null;
        }

        public JsonNode ObjectToJsonNode(object value) {
            if (value == null) {
                return null;
            }

            if (value is IDictionary<string, object> dict) {
                var obj = new JsonObject();
                foreach (KeyValuePair<string, object> kv in dict) {
                    obj[kv.Key] = kv.Value == null ? null : this.ObjectToJsonNode(kv.Value);
                }

                return obj;
            }

            if (value is IEnumerable enumerable and not string) {
                var arr = new JsonArray();
                foreach (object item in enumerable) {
                    arr.Add(item == null ? null : this.ObjectToJsonNode(item));
                }

                return arr;
            }

            return value switch {
                string s => JsonValue.Create(s),
                bool b => JsonValue.Create(b),
                int i => JsonValue.Create(i),
                long l => JsonValue.Create(l),
                float f => JsonValue.Create(f),
                double d => JsonValue.Create(d),
                decimal m => JsonValue.Create(m),
                DateTime dt => JsonValue.Create(dt.ToString("O")),
                DateOnly d => JsonValue.Create(d.ToString("O")),
                _ => RuntimeFeature.IsDynamicCodeSupported
                    ? this.ObjectToJsonNode(value.ToDictionary())
                    : throw new NotSupportedException($"Type '{value.GetType()}' Tidak AOT-safe JSON Serialization")
            };
        }

        public object JsonToObject(string json) {
            if (string.IsNullOrEmpty(json)) {
                return null;
            }

            var node = JsonNode.Parse(json);
            return this.JsonNodeToObject(node);
        }

        public string FormatByteSizeHumanReadable(long bytes, string forceUnit = null) {
            var dict = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase) {
                { "TB", 1000000000000 },
                { "GB", 1000000000 },
                { "MB", 1000000 },
                { "KB", 1000 },
                { "B", 1 }
            };

            long digit = 1;
            string ext = "B";
            if (!string.IsNullOrEmpty(forceUnit)) {
                digit = dict[forceUnit];
                ext = forceUnit;
            }
            else {
                foreach (KeyValuePair<string, long> kvp in dict) {
                    if (bytes > kvp.Value) {
                        digit = kvp.Value;
                        ext = kvp.Key;
                        break;
                    }
                }
            }

            return $"{(decimal)bytes / digit:0.00} {ext}";
        }

        public List<CDynamicClassProperty> GetTableClassStructureModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>() {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var ls = new List<CDynamicClassProperty>();

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties()) {
                Type type = Nullable.GetUnderlyingType(propertyInfo.PropertyType);

                Type dataType = type ?? propertyInfo.PropertyType;
                bool isNullable = type != null;

                if (type == null && dataType == typeof(string)) {
                    KeyAttribute primaryKey = propertyInfo.GetCustomAttribute<KeyAttribute>();
                    isNullable = primaryKey == null;
                }

                var item = new CDynamicClassProperty() {
                    ColumnName = propertyInfo.Name,
                    DataType = dataType.FullName,
                    IsNullable = isNullable
                };

                ls.Add(item);
            }

            return ls;
        }

        public List<CDynamicClassProperty> GetTableClassStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo) /* where T : JsonSerDe, new() */ {
            var ls = new List<CDynamicClassProperty>();

            foreach (JsonPropertyInfo prop in jsonTypeInfo.Properties) {
                Type type = prop.PropertyType;

                TypeRegistry.Register(type);

                Type underlyingType = Nullable.GetUnderlyingType(type);
                Type dataType = underlyingType ?? type;

                bool isNullable = underlyingType != null;

                if (underlyingType == null && dataType == typeof(string)) {
                    bool hasKeyAttribute = false;
                    if (prop.AttributeProvider != null) {
                        hasKeyAttribute = prop.AttributeProvider.IsDefined(typeof(KeyAttribute), false);
                    }

                    isNullable = !hasKeyAttribute;
                }

                var item = new CDynamicClassProperty() {
                    ColumnName = prop.Name,
                    DataType = dataType.FullName,
                    IsNullable = isNullable
                };

                ls.Add(item);
            }

            return ls;
        }

        public List<CDynamicClassPropertyV2> GetPocoStructureModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>() {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var list = new List<CDynamicClassPropertyV2>();

            foreach (PropertyInfo prop in typeof(T).GetProperties()) {
                Type type = prop.PropertyType;

                // Nullable<T> detection
                bool isNullable = Nullable.GetUnderlyingType(type) != null;
                Type coreType = isNullable ? Nullable.GetUnderlyingType(type) : type;

                bool isList =
                    coreType.IsGenericType &&
                    coreType.GetGenericTypeDefinition() == typeof(List<>);

                bool isDictionary =
                    coreType.IsGenericType &&
                    coreType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

                bool isArray = coreType.IsArray;
                bool isEnum = coreType.IsEnum;

                bool isClass =
                    coreType.IsClass &&
                    coreType != typeof(string) &&
                    !isDictionary &&
                    !isList &&
                    !isArray &&
                    !isEnum;

                var item = new CDynamicClassPropertyV2() {
                    ColumnName = prop.Name,
                    TypeName = coreType.FullName,
                    IsNullable = isNullable,
                    IsArray = isArray,
                    IsList = isList,
                    IsDictionary = isDictionary,
                    IsClass = isClass
                };

                list.Add(item);
            }

            return list;
        }

        public List<CDynamicClassPropertyV2> GetPocoStructureModel<T>(JsonTypeInfo<T> jsonTypeInfo) /* where T : JsonSerDe, new() */ {
            var list = new List<CDynamicClassPropertyV2>();

            foreach (JsonPropertyInfo prop in jsonTypeInfo.Properties) {
                Type type = prop.PropertyType;

                TypeRegistry.Register(type);

                Type underlyingType = Nullable.GetUnderlyingType(type);
                bool isNullable = underlyingType != null;
                Type coreType = underlyingType ?? type;

                bool isList = coreType.IsGenericType &&
                              coreType.GetGenericTypeDefinition() == typeof(List<>);

                bool isDictionary = coreType.IsGenericType &&
                                    coreType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

                bool isArray = coreType.IsArray;
                bool isEnum = coreType.IsEnum;

                bool isClass = coreType.IsClass &&
                               coreType != typeof(string) &&
                               !isDictionary &&
                               !isList &&
                               !isArray &&
                               !isEnum;

                var item = new CDynamicClassPropertyV2() {
                    ColumnName = prop.Name,
                    TypeName = coreType.FullName,
                    IsNullable = isNullable,
                    IsArray = isArray,
                    IsList = isList,
                    IsDictionary = isDictionary,
                    IsClass = isClass
                };

                list.Add(item);
            }

            return list;
        }

    }

}