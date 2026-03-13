using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Libraries {

    public sealed class EnumeratorObjectDataReader<T> : IDataReader {

        private readonly IEnumerator<T> _enumerator;
        private readonly bool _leaveOpen;
        private readonly int? _limit;
        private int _currentRowCount = 0;

        private readonly Func<T, object>[] _accessors;
        private readonly string[] _columnNames;
        private readonly Type[] _columnTypes;

        public EnumeratorObjectDataReader(IEnumerator<T> enumerator, JsonTypeInfo<T> jsonTypeInfo, int limit) {
            this._enumerator = enumerator;
            this._limit = limit;
            this._leaveOpen = true;

            IList<JsonPropertyInfo> props = jsonTypeInfo.Properties;
            int count = props.Count;
            this._columnNames = new string[count];
            this._columnTypes = new Type[count];
            this._accessors = new Func<T, object>[count];

            for (int i = 0; i < count; i++) {
                JsonPropertyInfo prop = props[i];
                this._columnNames[i] = prop.Name;
                this._columnTypes[i] = prop.PropertyType;
                this._accessors[i] = (item) => prop.Get?.Invoke(item) ?? DBNull.Value;
            }
        }

        public EnumeratorObjectDataReader(IEnumerator<T> enumerator, int limit) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            this._enumerator = enumerator;
            this._limit = limit;
            this._leaveOpen = true;

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int count = props.Length;
            this._columnNames = new string[count];
            this._columnTypes = new Type[count];
            this._accessors = new Func<T, object>[count];

            for (int i = 0; i < count; i++) {
                PropertyInfo prop = props[i];
                this._columnNames[i] = prop.Name;
                this._columnTypes[i] = prop.PropertyType;
                this._accessors[i] = (item) => prop.GetValue(item) ?? DBNull.Value;
            }
        }

        public bool Read() {
            if (this._limit.HasValue && this._currentRowCount >= this._limit.Value) {
                return false;
            }

            if (this._enumerator.MoveNext()) {
                this._currentRowCount++;
                return true;
            }

            return false;
        }

        public int FieldCount => this._accessors.Length;
        public string GetName(int i) => this._columnNames[i];
        public object GetValue(int i) => this._accessors[i](this._enumerator.Current);
        public int GetOrdinal(string name) => Array.FindIndex(this._columnNames, n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
        public Type GetFieldType(int i) => this._columnTypes[i];
        public string GetDataTypeName(int i) => this._columnTypes[i].Name;
        public bool IsDBNull(int i) => this.GetValue(i) == DBNull.Value;

        public void Close() {
            if (!this._leaveOpen) {
                this._enumerator.Dispose();
            }
        }

        public void Dispose() {
            if (!this._leaveOpen) {
                this._enumerator.Dispose();
            }
        }

        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => -1;

        public bool NextResult() => false;
        public bool GetBoolean(int i) => (bool)this.GetValue(i);
        public byte GetByte(int i) => (byte)this.GetValue(i);
        public char GetChar(int i) => (char)this.GetValue(i);
        public DateTime GetDateTime(int i) => (DateTime)this.GetValue(i);
        public decimal GetDecimal(int i) => (decimal)this.GetValue(i);
        public double GetDouble(int i) => (double)this.GetValue(i);
        public float GetFloat(int i) => (float)this.GetValue(i);
        public Guid GetGuid(int i) => (Guid)this.GetValue(i);
        public short GetInt16(int i) => (short)this.GetValue(i);
        public int GetInt32(int i) => (int)this.GetValue(i);
        public long GetInt64(int i) => (long)this.GetValue(i);
        public string GetString(int i) => this.GetValue(i).ToString();

        // Method IDataReader lainnya (Not Implemented)

        public object this[string name] => throw new NotImplementedException();
        public object this[int i] => throw new NotImplementedException();
        public DataTable GetSchemaTable() => throw new NotImplementedException();
        public long GetBytes(int i, long f, byte[] b, int bo, int l) => throw new NotImplementedException();
        public long GetChars(int i, long f, char[] b, int bo, int l) => throw new NotImplementedException();
        public IDataReader GetData(int i) => throw new NotImplementedException();
        public int GetValues(object[] values) => throw new NotImplementedException();

    }

}