using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class ObjectExtension {

        private static readonly HashSet<Type> ExtraSimpleTypes = new() {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset), // Recommended: Handles time zones
            typeof(TimeSpan),       // Recommended: Handles durations
            typeof(Guid)
        };

        public static bool IsSimpleType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || ExtraSimpleTypes.Contains(type);
        }

        public static Dictionary<string, object> ToDictionary<T>(this T instanceToConvert, JsonTypeInfo<T> jsonTypeInfo) {
            if (instanceToConvert == null) {
                return new Dictionary<string, object>();
            }

            return ConvertObject(instanceToConvert, jsonTypeInfo.Options) as Dictionary<string, object>
                   ?? new Dictionary<string, object>();
        }

        private static object ConvertObject(object obj, JsonSerializerOptions options) {
            if (obj == null) {
                return null;
            }

            if (obj is Type) {
                return obj;
            }

            Type type = obj.GetType();

            // 1. Check Simple Types
            if (IsSimpleType(type)) {
                return obj;
            }

            // 2. Check Dictionary
            if (obj is IDictionary dict) {
                return ConvertDictionary(dict, options);
            }

            // 3. Check List / Enumerable (but not string)
            if (obj is IEnumerable enumerable) {
                return ConvertEnumerable(enumerable, options);
            }

            // 4. Complex Object -> This is where we need the Lookup
            return ConvertComplexObject(obj, options);
        }

        private static Dictionary<string, object> ConvertDictionary(IDictionary dict, JsonSerializerOptions options) {
            var result = new Dictionary<string, object>();

            foreach (DictionaryEntry entry in dict) {
                string key = entry.Key.ToString();
                result[key] = ConvertObject(entry.Value, options);
            }

            result["IsCollection"] = true;
            return result;
        }

        private static Dictionary<string, object> ConvertEnumerable(IEnumerable enumerable, JsonSerializerOptions options) {
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            int index = 0;
            foreach (object item in enumerable) {
                result[index.ToString()] = ConvertObject(item, options);
                index++;
            }

            result["IsCollection"] = true;
            result["Count"] = index;
            return result;
        }

        private static Dictionary<string, object> ConvertComplexObject(object obj, JsonSerializerOptions options) {
            Type type = obj.GetType();

            JsonTypeInfo typeInfo = options.GetTypeInfo(type);

            if (typeInfo == null) {
                return new Dictionary<string, object>();
            }

            var result = new Dictionary<string, object>();

            foreach (JsonPropertyInfo prop in typeInfo.Properties) {
                if (prop.Get == null) {
                    continue; // Skip write-only
                }

                object value = prop.Get(obj);

                result[prop.Name] = ConvertObject(value, options);
            }

            result["IsCollection"] = false;
            return result;
        }

    }

}