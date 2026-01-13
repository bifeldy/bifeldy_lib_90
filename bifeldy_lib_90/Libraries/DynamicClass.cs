using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Libraries {

    // ==================================================================================
    // 1. TYPE REGISTRY (REQUIRED)
    // ==================================================================================
    public static class TypeRegistry {

        private static readonly Dictionary<string, Type> _knownTypes = new(StringComparer.OrdinalIgnoreCase);

        static TypeRegistry() {
            // 1. Reference Types
            // Auto-registers: T, T[], List<T>, Dictionary<string, T>
            RegisterReferenceType<object>();
            RegisterReferenceType<string>();

            // 2. Value Types
            // Auto-registers: T, T?, T[], T?[], List<T>, List<T?>, Dict<string, T>, Dict<string, T?>
            RegisterValueType<bool>();
            RegisterValueType<char>();
            RegisterValueType<byte>();
            RegisterValueType<sbyte>();
            RegisterValueType<short>();
            RegisterValueType<ushort>();
            RegisterValueType<int>();
            RegisterValueType<uint>();
            RegisterValueType<long>();
            RegisterValueType<ulong>();
            RegisterValueType<float>();
            RegisterValueType<double>();
            RegisterValueType<decimal>();

            RegisterValueType<Guid>();
            RegisterValueType<DateTime>();
            RegisterValueType<DateTimeOffset>();
            RegisterValueType<TimeSpan>();
            RegisterValueType<DateOnly>();
            RegisterValueType<TimeOnly>();

            // 3. Special Manual Registrations (Sparse arrays, lookups)
            Register<Dictionary<int, string>>();
            Register<Dictionary<int, object>>();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "We are explicitly registering types to ensure AOT generation.")]
        private static void RegisterReferenceType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : class {
            Register<T>();
            Register<T[]>();
            Register<List<T>>();
            Register<Dictionary<string, T>>();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "We are explicitly registering types to ensure AOT generation.")]
        private static void RegisterValueType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : struct {
            // Standard
            Register<T>();
            Register<T[]>();
            Register<List<T>>();
            Register<Dictionary<string, T>>();

            // Nullable
            Register<T?>();
            Register<T?[]>();
            Register<List<T?>>();
            Register<Dictionary<string, T?>>();
        }

        public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() {
            Register(typeof(T));
        }

        // 2. Non-Generic Entry Point
        public static void Register(Type t) {
            if (t == null) {
                return;
            }

            if (t.FullName != null && _knownTypes.ContainsKey(t.FullName)) {
                return;
            }

            // 1. Full Name
            if (t.FullName != null) {
                _knownTypes[t.FullName] = t;
            }

            // 2. Short Name
            _knownTypes[t.Name] = t;

            // 3. Assembly Qualified Name
            if (t.AssemblyQualifiedName != null) {
                _knownTypes[t.AssemblyQualifiedName] = t;
            }

            // 4. C# Aliases
            if (t == typeof(int)) {
                _knownTypes["int"] = t;
            }
            else if (t == typeof(string)) {
                _knownTypes["string"] = t;
            }
            else if (t == typeof(bool)) {
                _knownTypes["bool"] = t;
            }
            else if (t == typeof(byte)) {
                _knownTypes["byte"] = t;
            }
            else if (t == typeof(short)) {
                _knownTypes["short"] = t;
            }
            else if (t == typeof(long)) {
                _knownTypes["long"] = t;
            }
            else if (t == typeof(float)) {
                _knownTypes["float"] = t;
            }
            else if (t == typeof(double)) {
                _knownTypes["double"] = t;
            }
            else if (t == typeof(decimal)) {
                _knownTypes["decimal"] = t;
            }
            else if (t == typeof(object)) {
                _knownTypes["object"] = t;
            }

            // Recursively register inner types for Generics
            if (t.IsGenericType) {
                foreach (Type arg in t.GetGenericArguments()) {
                    Register(arg);
                }
            }

            // Recursively register Element types for Arrays
            if (t.IsArray) {
                Type elem = t.GetElementType();
                if (elem != null) {
                    Register(elem);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This is a best-effort fallback. The app should use Register<T> for safety.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern", Justification = "This is a best-effort fallback. The app should use Register<T> for safety.")]
        public static Type GetType(string typeName) {
            if (string.IsNullOrWhiteSpace(typeName)) {
                return null;
            }

            if (_knownTypes.TryGetValue(typeName, out Type t)) {
                return t;
            }

            try {
                // AOT Fallback (Best effort)
                return Type.GetType(typeName, false);
            }
            catch {
                return null;
            }
        }

    }

    // ==================================================================================
    // 2. CDynamicClass (Simple / Flat)
    // ==================================================================================
    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class CDynamicClassProperty : JsonSerDe {
        public string ColumnName { get; set; }
        public bool IsNullable { get; set; }
        public string DataType { get; set; }
    }

    [JsonSerializable(typeof(CDynamicClassProperty))]
    [JsonSerializable(typeof(CDynamicClassProperty[]))]
    [JsonSerializable(typeof(List<CDynamicClassProperty>))]
    [JsonSerializable(typeof(Dictionary<string, CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<CDynamicClassProperty>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<CDynamicClassProperty>))]
    [JsonSerializable(typeof(ResponseJsonSingle<CDynamicClassProperty>))]
    [JsonSerializable(typeof(ResponseJsonMulti<CDynamicClassProperty>))]
    public partial class CDynamicClassPropertyJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

    public sealed class CDynamicClass : IEnumerable<KeyValuePair<string, object>> {

        private readonly Dictionary<string, (Type Type, object Value)> _fields;

        public CDynamicClass(List<CDynamicClassProperty> fields) {
            this._fields = new Dictionary<string, (Type, object)>(StringComparer.OrdinalIgnoreCase);

            foreach (CDynamicClassProperty f in fields) {
                Type type = TypeRegistry.GetType(f.DataType);
                if (type == null) {
                    throw new Exception($"Unknown type '{f.DataType}'. Register it in TypeRegistry.");
                }

                // AOT SAFE: Do not use MakeGenericType. We just store the type as-is and handle nulls manually.
                this._fields[f.ColumnName] = (type, null);
            }
        }

        public object this[string key] {
            get {
                if (this._fields.TryGetValue(key, out (Type Type, object Value) field)) {
                    return field.Value;
                }

                throw new KeyNotFoundException($"Field '{key}' does not exist.");
            }
            set {
                if (!this._fields.TryGetValue(key, out (Type Type, object Value) field)) {
                    throw new KeyNotFoundException($"Field '{key}' does not exist.");
                }

                Type underlying = Nullable.GetUnderlyingType(field.Type) ?? field.Type;

                if (value == null) {
                    // In a real app, you might check f.IsNullable here, but for now we allow nulls in object storage.
                    this._fields[key] = (field.Type, null);
                }
                else {
                    try {
                        object converted = Convert.ChangeType(value, underlying);
                        this._fields[key] = (field.Type, converted);
                    }
                    catch (Exception ex) {
                        throw new Exception($"Cannot convert '{value}' to type '{underlying.Name}' for field '{key}'", ex);
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            foreach (KeyValuePair<string, (Type Type, object Value)> kvp in this._fields) {
                yield return new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    }

    // ==================================================================================
    // 3. CDynamicClassV2 (Complex / Recursive)
    // ==================================================================================
    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class CDynamicClassPropertyV2 : JsonSerDe {
        public string ColumnName { get; set; }
        public bool IsNullable { get; set; }
        public string TypeName { get; set; }
        public bool IsArray { get; set; }
        public bool IsList { get; set; }
        public bool IsDictionary { get; set; }
        public bool IsClass { get; set; }
    }

    [JsonSerializable(typeof(CDynamicClassPropertyV2))]
    [JsonSerializable(typeof(CDynamicClassPropertyV2[]))]
    [JsonSerializable(typeof(List<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(Dictionary<string, CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(ResponseJsonSingle<CDynamicClassPropertyV2>))]
    [JsonSerializable(typeof(ResponseJsonMulti<CDynamicClassPropertyV2>))]
    public partial class CDynamicClassPropertyV2JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

    public sealed class CDynamicClassV2 : IEnumerable<KeyValuePair<string, object>> {

        private readonly Dictionary<string, (Type Type, object Value, CDynamicClassPropertyV2 Meta)> _fields;

        public CDynamicClassV2(List<CDynamicClassPropertyV2> fields) {
            this._fields = new Dictionary<string, (Type, object, CDynamicClassPropertyV2)>(StringComparer.OrdinalIgnoreCase);

            foreach (CDynamicClassPropertyV2 f in fields) {
                Type type = TypeRegistry.GetType(f.TypeName);

                if (type == null) {
                    if (f.IsList) {
                        type = typeof(List<object>);
                    }
                    else if (f.IsArray) {
                        type = typeof(object[]);
                    }
                    else {
                        type = typeof(Dictionary<string, object>);
                    }
                }

                this._fields[f.ColumnName] = (type, null, f);
            }
        }

        public object this[string key] {
            get {
                if (this._fields.TryGetValue(key, out (Type Type, object Value, CDynamicClassPropertyV2 Meta) field)) {
                    return field.Value;
                }

                throw new KeyNotFoundException($"Field '{key}' does not exist.");
            }
            set {
                if (!this._fields.TryGetValue(key, out (Type Type, object Value, CDynamicClassPropertyV2 Meta) field)) {
                    throw new KeyNotFoundException($"Field '{key}' does not exist.");
                }

                try {
                    object converted = this.ConvertValue(field.Meta, field.Type, value);
                    this._fields[key] = (field.Type, converted, field.Meta);
                }
                catch (Exception ex) {
                    throw new Exception($"Error setting '{key}': {ex.Message}", ex);
                }
            }
        }

        public bool HasProperty(string name) => this._fields.ContainsKey(name);
        public IEnumerable<string> Keys => this._fields.Keys;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safe via TypeRegistry")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode", Justification = "Safe via TypeRegistry")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "Safe via TypeRegistry")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "Safe via TypeRegistry")]
        private object ConvertValue(CDynamicClassPropertyV2 meta, Type type, object value) {
            if (value == null) {
                return null;
            }

            Type coreType = Nullable.GetUnderlyingType(type) ?? type;

            if (coreType.IsEnum) {
                return Enum.Parse(coreType, value.ToString()!, true);
            }

            // --- LIST ---
            if (meta.IsList) {
                var list = (IList)Activator.CreateInstance(type);
                if (list == null) {
                    return null;
                }

                Type innerType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);

                if (value is IEnumerable source) {
                    foreach (object item in source) {
                        // FIX: Gunakan Helper CreateNestedMeta
                        object converted = this.ConvertValue(
                            this.CreateNestedMeta(innerType),
                            innerType,
                            item
                        );

                        _ = list.Add(converted);
                    }
                }

                return list;
            }

            // --- ARRAY ---
            if (meta.IsArray) {
                var sourceList = ((IEnumerable)value).Cast<object>().ToList();
                Type innerType = coreType.GetElementType() ?? typeof(object);
                var array = Array.CreateInstance(innerType, sourceList.Count);

                for (int i = 0; i < sourceList.Count; i++) {
                    object converted = this.ConvertValue(
                        this.CreateNestedMeta(innerType),
                        innerType,
                        sourceList[i]
                    );

                    array.SetValue(converted, i);
                }

                return array;
            }

            // --- DICTIONARY ---
            if (meta.IsDictionary) {
                var dict = (IDictionary)Activator.CreateInstance(type);
                if (dict == null) {
                    return null;
                }

                Type keyType = type.GetGenericArguments()[0];
                Type valType = type.GetGenericArguments()[1];

                foreach (DictionaryEntry entry in (IDictionary)value) {
                    object k = this.ConvertValue(this.CreateNestedMeta(keyType), keyType, entry.Key);
                    object v = this.ConvertValue(this.CreateNestedMeta(valType), valType, entry.Value);

                    if (k != null) {
                        dict.Add(k, v);
                    }
                }

                return dict;
            }

            // --- NESTED CLASS ---
            if (meta.IsClass && coreType != typeof(string)) {
                if (value is IDictionary<string, object> dict) {
                    var nestedMeta = dict.Select(x => {
                        Type t = x.Value?.GetType() ?? typeof(object);
                        CDynamicClassPropertyV2 m = this.CreateNestedMeta(t);
                        m.ColumnName = x.Key;
                        return m;
                    }).ToList();

                    var nestedClass = new CDynamicClassV2(nestedMeta);
                    foreach (KeyValuePair<string, object> kvp in dict) {
                        nestedClass[kvp.Key] = kvp.Value;
                    }

                    return nestedClass;
                }

                return value;
            }

            return Convert.ChangeType(value, coreType);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            foreach (KeyValuePair<string, (Type Type, object Value, CDynamicClassPropertyV2 Meta)> kvp in this._fields) {
                yield return new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private CDynamicClassPropertyV2 CreateNestedMeta(Type type) {
            Type core = Nullable.GetUnderlyingType(type) ?? type;

            bool isList = core.IsGenericType && core.GetGenericTypeDefinition() == typeof(List<>);
            bool isDict = core.IsGenericType && core.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            bool isArray = core.IsArray;

            // IsClass true jika dia class, tapi bukan string, bukan list, dan bukan dictionary
            bool isClass = core.IsClass && core != typeof(string) && !isArray && !isList && !isDict;

            return new CDynamicClassPropertyV2 {
                TypeName = type.AssemblyQualifiedName!,
                IsList = isList,
                IsArray = isArray,
                IsDictionary = isDict,
                IsClass = isClass,
                IsNullable = Nullable.GetUnderlyingType(type) != null
            };
        }

    }

}