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

}