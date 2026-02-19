using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class API_KEY_T : JsonSerDe {
        [Key] public string KEY { set; get; }
        public string IP_ORIGIN { set; get; }
        [Key] public string APP_NAME { set; get; }
        public string KETER { set; get; }
    }

    [JsonSerializable(typeof(API_KEY_T))]
    [JsonSerializable(typeof(API_KEY_T[]))]
    [JsonSerializable(typeof(List<API_KEY_T>))]
    [JsonSerializable(typeof(Dictionary<string, API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<API_KEY_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<API_KEY_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<API_KEY_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<API_KEY_T>))]
    public partial class API_KEY_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}