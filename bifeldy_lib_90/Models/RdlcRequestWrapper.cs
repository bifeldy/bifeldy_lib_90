using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class RdlcInfoWrapper(IDictionary<string, string> parameters) : JsonSerDe {
        public string DataFilePath { get; set; }
        public IDictionary<string, string> Parameters { get; set; } = parameters;
    }

    [JsonSerializable(typeof(RdlcInfoWrapper))]
    [JsonSerializable(typeof(RdlcInfoWrapper[]))]
    [JsonSerializable(typeof(List<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(Dictionary<string, RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(ResponseJsonSingle<RdlcInfoWrapper>))]
    [JsonSerializable(typeof(ResponseJsonMulti<RdlcInfoWrapper>))]
    public partial class RdlcInfoWrapperJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}