using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class DC_TABEL_DC_T : JsonSerDe {
        [Key] public decimal TBL_DCID { get; set; }
        public string TBL_DC_KODE { get; set; }
        public string TBL_DC_NAMA { get; set; }
        public string TBL_UPDREC_ID { get; set; }
        public DateTime? TBL_UPDREC_DATE { get; set; }
        public string TBL_DC_MCG { get; set; }
        public string TBL_TAG_ERROR_BELI { get; set; }
        public string TBL_NPWP_DC { get; set; }
        public string TBL_CABANG_KODE { get; set; }
        public string TBL_CABANG_NAMA { get; set; }
        public DateTime? TBL_TGL_BUKA { get; set; }
        public string TBL_SINGKATAN { get; set; }
        public string TBL_JENIS_DC { get; set; }
        public string TBL_START_SYNC { get; set; }
        public string TBL_ORACLE { get; set; }
        public string TBL_CLIPPER { get; set; }
        public DateTime? TBL_TGL_TUTUP { get; set; }
        public string TBL_DBLINK_EIS { get; set; }
        public string TBL_DC_INDUK { get; set; }
        public decimal? MAX_PKM_OTOMATIS { get; set; }
        public decimal? MAX_PKM_MANUAL { get; set; }
        public DateTime? TBL_8DIGIT { get; set; }
        public decimal? TBL_TW { get; set; }
        // public string FLAG_CSV { get; set; }
        // public string TEMP_CSV { get; set; }
        // public string PO_CSV { get; set; }
        // public decimal? TBL_PROCESS { get; set; }
        // public string FLAG_JAWA { get; set; }
        // public string ZONA_WAKTU { get; set; }
    }

    [JsonSerializable(typeof(DC_TABEL_DC_T))]
    [JsonSerializable(typeof(DC_TABEL_DC_T[]))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DC_TABEL_DC_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DC_TABEL_DC_T>))]
    public partial class DC_TABEL_DC_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}