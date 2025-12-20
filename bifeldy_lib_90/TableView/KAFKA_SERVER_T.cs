using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class KAFKA_SERVER_T : JsonSerDe {
        [Key] public string HOST { get; set; }
        [Key] public decimal PORT { get; set; }
        [Key] public string TOPIC { get; set; }
        public string GROUP_ID { get; set; }
        public decimal? REPLI { get; set; }
        public decimal? PARTI { get; set; }
    }

    [JsonSerializable(typeof(KAFKA_SERVER_T))]
    [JsonSerializable(typeof(KAFKA_SERVER_T[]))]
    [JsonSerializable(typeof(List<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(Dictionary<string, KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<KAFKA_SERVER_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<KAFKA_SERVER_T>))]
    public partial class KAFKA_SERVER_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}
