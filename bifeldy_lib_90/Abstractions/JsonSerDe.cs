using bifeldy_lib_90.Filters;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Abstractions {

    // Bikin Class Turunan ?? Jangan Lupa Samain `Attributnya`
    // Dan Buat Juga `SerializerContextnya` Lalu Daftarkan Ke `JsonSerializationExtension`

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public abstract class JsonSerDe : IOpenApiPropertyFilter {
        public virtual string[] HiddenProperties() => [];
    }

    [JsonSerializable(typeof(JsonSerDe))]
    [JsonSerializable(typeof(JsonSerDe[]))]
    [JsonSerializable(typeof(List<JsonSerDe>))]
    [JsonSerializable(typeof(Dictionary<string, JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<JsonSerDe>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<JsonSerDe>))]
    [JsonSerializable(typeof(ResponseJsonSingle<JsonSerDe>))]
    [JsonSerializable(typeof(ResponseJsonMulti<JsonSerDe>))]
    public partial class JsonSerDeJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}
