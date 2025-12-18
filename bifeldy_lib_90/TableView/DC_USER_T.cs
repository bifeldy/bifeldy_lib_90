using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class DC_USER_T : JsonSerDe {
        [Key] public string USER_NAME { get; set; }
        public string USER_PASSWORD { get; set; }
        public string USER_APP_MODUL { get; set; }
        public string USER_PRIVS { get; set; }
        public string USER_GROUP { get; set; }
        public decimal? USER_FK_TBL_DCID { get; set; }
        public DateTime? USER_UPDREC_DATE { get; set; }
        public string USER_UPDREC_ID { get; set; }
        public decimal? USER_FK_TBL_LOKASIID { get; set; }
        public decimal? USER_FK_TBL_GUDANGID { get; set; }
        public decimal? USER_FK_TBL_DEPOID { get; set; }
        public string USER_FLAG_HANDHELD { get; set; }
        public string USER_NIK { get; set; }
        public string USER_FLAG_HO { get; set; }
        public DateTime? LAST_PASS_CHANGE { get; set; }
        public decimal? PASS_VALID_DAYS { get; set; }

        public override string[] HiddenProperties() => ["USER_PASSWORD"];
    }

    [JsonSerializable(typeof(DC_USER_T))]
    [JsonSerializable(typeof(DC_USER_T[]))]
    [JsonSerializable(typeof(List<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DC_USER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DC_USER_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DC_USER_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DC_USER_T>))]
    public partial class DC_USER_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}