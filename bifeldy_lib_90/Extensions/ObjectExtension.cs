using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Extensions {

    public static class ObjectExtension {

        private static readonly HashSet<Type> ExtraSimpleTypes = [
            typeof(byte[]),
            typeof(string),
            typeof(decimal),
            typeof(DateOnly),
            typeof(DateTime),
            typeof(DateTimeOffset), // Recommended: Handles time zones
            typeof(TimeOnly),
            typeof(TimeSpan),       // Recommended: Handles durations
            typeof(Guid)
        ];

        public static bool IsSimpleType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsArray && type != typeof(byte[])) {
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
                return true;
            }

            return type.IsPrimitive || type.IsEnum || ExtraSimpleTypes.Contains(type);
        }

        public static Dictionary<string, object> ToDictionary<T>(this T instanceToConvert) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new DecimalConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalConverter());

            if (instanceToConvert == null) {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            return ConvertObject(instanceToConvert, jsonSerializerOptions) as Dictionary<string, object> ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, object> ToDictionary<T>(this T instanceToConvert, JsonTypeInfo<T> jsonTypeInfo) where T : JsonSerDe, new() {
            if (instanceToConvert == null) {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            return ConvertObject(instanceToConvert, jsonTypeInfo.Options) as Dictionary<string, object> ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (DictionaryEntry entry in dict) {
                string key = entry.Key.ToString();
                result[key] = ConvertObject(entry.Value, options);
            }

            result["IsCollection"] = true;
            return result;
        }

        private static Dictionary<string, object> ConvertEnumerable(IEnumerable enumerable, JsonSerializerOptions options) {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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

        public static Action<object, object> CreateSetter(PropertyInfo property) {
            // Parameters: (object target, object value)
            ParameterExpression targetExpr = Expression.Parameter(typeof(object), "target");
            ParameterExpression valueExpr = Expression.Parameter(typeof(object), "value");

            // Cast the 'target' to the specific class type: (T)target
            UnaryExpression castTargetExpr = Expression.Convert(targetExpr, property.DeclaringType);

            // Cast the 'value' to the property's type: (PropertyType)value
            UnaryExpression castValueExpr = Expression.Convert(valueExpr, property.PropertyType);

            // Build the assignment: ((T)target).Property = (PropertyType)value
            MemberExpression propertyExpr = Expression.Property(castTargetExpr, property);
            BinaryExpression assignExpr = Expression.Assign(propertyExpr, castValueExpr);

            // Compile into Action<object, object>
            return Expression.Lambda<Action<object, object>>(assignExpr, targetExpr, valueExpr).Compile();
        }

    }

}