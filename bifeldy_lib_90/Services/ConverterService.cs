using bifeldy_lib_90.Libraries;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace bifeldy_lib_90.Services {

    public interface IConverterService {
        byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument);
        T JsonToObject<T>(string json, JsonTypeInfo<T> typeInfo);
        string ObjectToJson<T>(T value, JsonTypeInfo<T> typeInfo);
        string XmlToJson(string xml);
        object JsonNodeToObject(JsonNode node);
        JsonNode ObjectToJsonNode(object value);
        object JsonToObject(string json);
        string ObjectToJson(object value);
        string FormatByteSizeHumanReadable(long bytes, string forceUnit = null);
    }

    public sealed class CConverterService : IConverterService {

        private readonly IConverter _converter;

        public CConverterService(IConverter converter) {
            this._converter = converter;
        }

        // Unfortunately the main wkhtmltopdf project is not active any more.
        // https://github.com/HakanL/WkHtmlToPdf-DotNet/issues/132
        public byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument) {
            return this._converter.Convert(htmlToPdfDocument);
        }

        public T JsonToObject<T>(string json, JsonTypeInfo<T> typeInfo) {
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize(json, typeInfo);
        }

        public string ObjectToJson<T>(T value, JsonTypeInfo<T> typeInfo) {
            return value == null ? null : JsonSerializer.Serialize(value, typeInfo);
        }

        private JsonNode ConvertXmlToJsonNode(XElement element) {
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
                        jsonObject.Add(group.Key, this.ConvertXmlToJsonNode(child));
                    }
                }
                else {
                    var jsonArray = new JsonArray();
                    foreach (XElement child in group) {
                        jsonArray.Add(this.ConvertXmlToJsonNode(child));
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
            var xdoc = XDocument.Parse(xml);
            xdoc.Declaration = null;

            JsonNode jsonNode = this.ConvertXmlToJsonNode(xdoc.Root);
            return jsonNode.ToJsonString();
        }

        public object JsonNodeToObject(JsonNode node) {
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

                throw new NotSupportedException("Unsupported JSON primitive");
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
                _ => throw new NotSupportedException($"Type '{value.GetType()}' is not supported in AOT-safe JSON serialization")
            };
        }

        public object JsonToObject(string json) {
            if (string.IsNullOrWhiteSpace(json)) {
                return null;
            }

            var node = JsonNode.Parse(json);
            return this.JsonNodeToObject(node);
        }

        public string ObjectToJson(object value) {
            if (value == null) {
                return null;
            }

            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new DecimalConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalConverter());

            return this.ObjectToJsonNode(value).ToJsonString(jsonSerializerOptions);
        }

        public string FormatByteSizeHumanReadable(long bytes, string forceUnit = null) {
            IDictionary<string, long> dict = new Dictionary<string, long>(StringComparer.InvariantCultureIgnoreCase) {
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

            return $"{(decimal) bytes / digit:0.00} {ext}";
        }

    }

}
