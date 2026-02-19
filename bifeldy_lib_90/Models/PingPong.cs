using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class InputJsonDcPingPong : InputJsonDc {
        public string version { get; set; }
        public decimal port_api { get; set; }
        public decimal port_grpc { get; set; }
        public string app_name { get; set; }
    }

    [JsonSerializable(typeof(InputJsonDcPingPong))]
    [JsonSerializable(typeof(InputJsonDcPingPong[]))]
    [JsonSerializable(typeof(List<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(Dictionary<string, InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(ResponseJsonSingle<InputJsonDcPingPong>))]
    [JsonSerializable(typeof(ResponseJsonMulti<InputJsonDcPingPong>))]
    public partial class InputJsonDcPingPongJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}