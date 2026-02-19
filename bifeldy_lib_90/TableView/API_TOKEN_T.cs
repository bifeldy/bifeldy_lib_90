using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    public sealed class API_TOKEN_T : JsonSerDe {
        [Key] public string USER_NAME { set; get; }
        public string PASSWORD { set; get; }
        [Key] public string APP_NAME { set; get; }
        public DateTime? LAST_LOGIN { set; get; }
        public string TOKEN_SEKALI_PAKAI { set; get; }

        public override string[] HiddenProperties() => [
            nameof(this.TOKEN_SEKALI_PAKAI)
        ];
    }

    [JsonSerializable(typeof(API_TOKEN_T))]
    [JsonSerializable(typeof(API_TOKEN_T[]))]
    [JsonSerializable(typeof(List<API_TOKEN_T>))]
    [JsonSerializable(typeof(Dictionary<string, API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<API_TOKEN_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<API_TOKEN_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<API_TOKEN_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<API_TOKEN_T>))]
    public partial class API_TOKEN_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}