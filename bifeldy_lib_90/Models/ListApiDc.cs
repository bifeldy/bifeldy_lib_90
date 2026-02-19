using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class ListApiDc : JsonSerDe {
        public string DC_KODE { get; set; }
        public string FLAG_DBPG { get; set; }
        public string IP_NGINX { get; set; }
        public string USER_NGINX { get; set; }
        public string PASS_NGINX { get; set; }
        public string APP_NAME { get; set; }
        public string API_HOST { get; set; }
        public string API_PATH { get; set; }
        public DateTime? LAST_ONLINE { get; set; }
        public string VERSION { get; set; }
        public decimal? PORT_GRPC { get; set; }
        public string DEFAULT_API_PATH { get; set; }
        public decimal? PING_PONG { get; set; }

        public override string[] HiddenProperties() => [
            nameof(this.USER_NGINX),
            nameof(this.PASS_NGINX),
        ];
    }

    [JsonSerializable(typeof(ListApiDc))]
    [JsonSerializable(typeof(ListApiDc[]))]
    [JsonSerializable(typeof(List<ListApiDc>))]
    [JsonSerializable(typeof(Dictionary<string, ListApiDc>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<ListApiDc>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<ListApiDc>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<ListApiDc>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<ListApiDc>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<ListApiDc>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<ListApiDc>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ListApiDc>))]
    [JsonSerializable(typeof(ResponseJsonMulti<ListApiDc>))]
    public partial class ListApiDcJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}