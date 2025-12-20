using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class ServerConfigKunci : JsonSerDe {
        public string kode_dc { get; set; }
        public string kunci_gxxx { get; set; }
        public string server_target { get; set; }
    }

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class ServerConfigAddEditDelete : InputJson {
        public string kode_dc { get; set; }
        public string kunci_gxxx { get; set; }
        public string server_target { get; set; }
        public string password { get; set; }
        public string type { get; set; } = "EDIT"; // "ADD", "DELETE"
    }

    [JsonSerializable(typeof(ServerConfigKunci))]
    [JsonSerializable(typeof(ServerConfigKunci[]))]
    [JsonSerializable(typeof(List<ServerConfigKunci>))]
    [JsonSerializable(typeof(Dictionary<string, ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<ServerConfigKunci>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<ServerConfigKunci>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ServerConfigKunci>))]
    [JsonSerializable(typeof(ResponseJsonMulti<ServerConfigKunci>))]
    [JsonSerializable(typeof(ServerConfigAddEditDelete))]
    [JsonSerializable(typeof(ServerConfigAddEditDelete[]))]
    [JsonSerializable(typeof(List<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(Dictionary<string, ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ServerConfigAddEditDelete>))]
    [JsonSerializable(typeof(ResponseJsonMulti<ServerConfigAddEditDelete>))]
    public partial class ServerConfigJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}