using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public sealed class KafkaInstance {
        public string HOST_PORT { get; set; }
        public string TOPIC { get; set; }
        public string LOG_TABLE_NAME { get; set; }
        public string GROUP_ID { get; set; }
        public bool SUFFIX_KODE_DC { get; set; }
        public short REPLICATION { get; set; } = -1;
        public int PARTITION { get; set; } = -1;
        public string EXCLUDE_JENIS_DC { get; set; }
    }

    [JsonSerializable(typeof(KafkaInstance))]
    [JsonSerializable(typeof(KafkaInstance[]))]
    [JsonSerializable(typeof(List<KafkaInstance>))]
    [JsonSerializable(typeof(Dictionary<string, KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<KafkaInstance>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<KafkaInstance>))]
    [JsonSerializable(typeof(ResponseJsonSingle<KafkaInstance>))]
    [JsonSerializable(typeof(ResponseJsonMulti<KafkaInstance>))]
    public partial class KafkaInstanceJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}