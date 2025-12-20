using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class DC_TABEL_V : JsonSerDe {
        public string TBL_DC_KODE { get; set; }
        public string TBL_DC_NAMA { get; set; }
        public string TBL_JENIS_DC { get; set; }
        public string TBL_DC_INDUK { get; set; }
        public string IP_DB { get; set; }
        public string DB_USER_NAME { get; set; }
        public string DB_PASSWORD { get; set; }
        public decimal? DB_PORT { get; set; }
        public string DB_SID { get; set; }
        public string DBPG_IP { get; set; }
        public string DBPG_NAME { get; set; }
        public string DBPG_USER { get; set; }
        public string DBPG_PASS { get; set; }
        public string DBPG_PORT { get; set; }
        public string FLAG_DBPG { get; set; }
    }

    [JsonSerializable(typeof(DC_TABEL_V))]
    [JsonSerializable(typeof(DC_TABEL_V[]))]
    [JsonSerializable(typeof(List<DC_TABEL_V>))]
    [JsonSerializable(typeof(Dictionary<string, DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DC_TABEL_V>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DC_TABEL_V>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DC_TABEL_V>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DC_TABEL_V>))]
    public partial class DC_TABEL_V_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}
