using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class InfoDc : JsonSerDe {
        public string kode_dc { get; set; }
        public string nama_dc { get; set; }
        public string jenis_dc { get; set; }
        public string app_name { get; set; }
        public string app_version { get; set; }
        public string client_ip { get; set; }
    }

    [JsonSerializable(typeof(InfoDc))]
    [JsonSerializable(typeof(InfoDc[]))]
    [JsonSerializable(typeof(List<InfoDc>))]
    [JsonSerializable(typeof(Dictionary<string, InfoDc>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<InfoDc>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<InfoDc>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<InfoDc>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<InfoDc>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<InfoDc>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<InfoDc>))]
    [JsonSerializable(typeof(ResponseJsonSingle<InfoDc>))]
    [JsonSerializable(typeof(ResponseJsonMulti<InfoDc>))]
    public partial class InfoDcJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}
