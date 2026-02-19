using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class KunciRequest : JsonSerDe {
        public string Key { get; set; }
    }

    [JsonSerializable(typeof(KunciRequest))]
    [JsonSerializable(typeof(KunciRequest[]))]
    [JsonSerializable(typeof(List<KunciRequest>))]
    [JsonSerializable(typeof(Dictionary<string, KunciRequest>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<KunciRequest>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<KunciRequest>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<KunciRequest>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<KunciRequest>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<KunciRequest>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<KunciRequest>))]
    [JsonSerializable(typeof(ResponseJsonSingle<KunciRequest>))]
    [JsonSerializable(typeof(ResponseJsonMulti<KunciRequest>))]
    public partial class KunciRequestJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}