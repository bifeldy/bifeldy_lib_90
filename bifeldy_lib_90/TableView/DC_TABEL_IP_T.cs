using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class DC_TABEL_IP_T : JsonSerDe {
        public decimal? DCID { get; set; }
        public string DC_KODE { get; set; }
        public string IP_DB { get; set; }
        public string DB_USER_NAME { get; set; }
        public string DB_PASSWORD { get; set; }
        public string DB_USER_VIEW { get; set; }
        public string DB_PASS_VIEW { get; set; }
        public string DB_SID { get; set; }
        public decimal? DB_PORT { get; set; }
        public string IP_IAS { get; set; }
        public string IAS_USER_NAME { get; set; }
        public string IAS_PASSWORD { get; set; }
        public string IAS_TNS { get; set; }
        public string IAS_FORM { get; set; }
        public string IAS_REPORT { get; set; }
        public string IP_DB_850 { get; set; }
        public string DB_850_USER_NAME { get; set; }
        public string DB_850_PASSWORD { get; set; }
        public string DB_850_SID { get; set; }
        public string IP_DB_SIM { get; set; }
        public string DB_SIM_USER_NAME { get; set; }
        public string DB_SIM_PASSWORD { get; set; }
        public string DB_SIM_SID { get; set; }
        public string IP_SIM_IAS { get; set; }
        public string FLAG_LIVE { get; set; }
        public string DB_IP_IIS { get; set; }
        public string DB_USER_IIS { get; set; }
        public string DB_PASS_IIS { get; set; }
        public string DB_PASS_IIS_OLD { get; set; }
        public string DB_IP_SQL { get; set; }
        public string DB_PWD_SQL { get; set; }
        public string IP_SYNCDATA { get; set; }
        public string TNS_NAME { get; set; }
        public string SCHEMA_DPD { get; set; }
        public DateTime? UPDREC_DATE { get; set; }
        public string TNS_TOAD { get; set; }
        public string FTP_FIN_FOLDER { get; set; }
        public string DIR_LIS { get; set; }
        public string FTP_IIS_ID { get; set; }
        public string FTP_IIS_PASS { get; set; }
        public string FTP_PROGVB_ID { get; set; }
        public string FTP_PROGVB_PASS { get; set; }
        public string DIR_LIS_APP { get; set; }
        public string FTP_KUNCI_NAME { get; set; }
        public string DIR_LIS_AUTO { get; set; }
        public string DB_PASS_OLD { get; set; }
        public DateTime? LAST_PASS_CHANGE { get; set; }
        public string TNS_TOAD_850 { get; set; }
        public DateTime? TGL_TUTUP { get; set; }
        public DateTime? TGL_TUTUP_SEMENTARA { get; set; }
        public string DB1_SERVICETAG { get; set; }
        public string IAS_SERVICETAG { get; set; }
        public DateTime? LAST_PASSIIS_CHANGE { get; set; }
        public string IIS_USER_DC { get; set; }
        public string IIS_PASS_DC { get; set; }
        public string PASS_SA_SQLSERVER { get; set; }
        public string DB_USER_SQL { get; set; }
        public string USER_VIRTUALBOX { get; set; }
        public string PASS_VIRTUALBOX { get; set; }
        public string FLAG_IAS_VIRTUAL { get; set; }
        public string FLAG_IIS_VIRTUAL { get; set; }
        public string NOTE { get; set; }
        public string DPD_SERVICETAG { get; set; }
        public string IIS_SERVICETAG { get; set; }
        public string IP_NGINX { get; set; }
        public string USER_NGINX { get; set; }
        public string PASS_NGINX { get; set; }
        public string DB2_SERVICETAG { get; set; }
        public string SIM_SERVICETAG { get; set; }
        public string IP_DB2 { get; set; }
        public string FLAG_FORMASI_5SERVER { get; set; }
        public string THNBLN_PEREMAJAAN { get; set; }
        public string FLAG_STORAGECRAFT { get; set; }
        public string URL_LIS64 { get; set; }
        public string IDRAC_DB { get; set; }
        public string IDRAC_DB2 { get; set; }
        public string IDRAC_IAS { get; set; }
        public string IDRAC_DPD { get; set; }
        public string IPASAL_STORAGECRAFT { get; set; }
        public string IPTUJUAN_STORAGECRAFT { get; set; }
        public string LIS64_IP { get; set; }
        public string LIS64_USER { get; set; }
        public string LIS64_PASS { get; set; }
        public string LIS64_NOTE { get; set; }
        public string REGIONAL { get; set; }
        public string TYPED_SERVICETAG { get; set; }
        public string TYPED_IP { get; set; }
        public string TYPED_USER { get; set; }
        public string TYPED_PASS { get; set; }
        public string FLAG_DBPG { get; set; }
        public string DBPG_IP { get; set; }
        public string DBPG_USER { get; set; }
        public string DBPG_PASS { get; set; }
        public string DBPG_NAME { get; set; }
        public string DBPG_PORT { get; set; }
        public string DBPG_SCHEMA { get; set; }
        public string NGINX_DIR_LIS { get; set; }
        public string FTP_NGINX_ID { get; set; }
        public string FTP_NGINX_PASS { get; set; }
        public string SYNC_NGINX { get; set; }
        public string NOTE_NGINX { get; set; }
        public string PASS_NGINX_OLD { get; set; }
        public DateTime? LAST_PASSNGINX_CHANGE { get; set; }
        public string FLAG_PICKING { get; set; }
        public DateTime? DBPG_TGLMIGRASI { get; set; }
        public string IP_EMBULK { get; set; }
        public string FLAG_DOCKER { get; set; }
        public string IP_KAFKA_CONNECT { get; set; }
        public string PORT_KAFKA_CONNECT { get; set; }
        public string CATATAN { get; set; }
    }

    [JsonSerializable(typeof(DC_TABEL_IP_T))]
    [JsonSerializable(typeof(DC_TABEL_IP_T[]))]
    [JsonSerializable(typeof(List<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(Dictionary<string, DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DC_TABEL_IP_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DC_TABEL_IP_T>))]
    public partial class DC_TABEL_IP_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}