using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class CTableClassModel : JsonSerDe {
        public string table_name { get; set; }
        public List<CDynamicClassProperty> properties { get; set; }
    }

    [JsonSerializable(typeof(CTableClassModel))]
    [JsonSerializable(typeof(CTableClassModel[]))]
    [JsonSerializable(typeof(List<CTableClassModel>))]
    [JsonSerializable(typeof(Dictionary<string, CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<CTableClassModel>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<CTableClassModel>))]
    [JsonSerializable(typeof(ResponseJsonSingle<CTableClassModel>))]
    [JsonSerializable(typeof(ResponseJsonMulti<CTableClassModel>))]
    public partial class CTableClassModelJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class CPocoModel : JsonSerDe {
        public string poco_name { get; set; }
        public List<CDynamicClassPropertyV2> properties { get; set; }
    }

    [JsonSerializable(typeof(CPocoModel))]
    [JsonSerializable(typeof(CPocoModel[]))]
    [JsonSerializable(typeof(List<CPocoModel>))]
    [JsonSerializable(typeof(Dictionary<string, CPocoModel>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<CPocoModel>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<CPocoModel>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<CPocoModel>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<CPocoModel>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<CPocoModel>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<CPocoModel>))]
    [JsonSerializable(typeof(ResponseJsonSingle<CPocoModel>))]
    [JsonSerializable(typeof(ResponseJsonMulti<CPocoModel>))]
    public partial class CPocoModelJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}