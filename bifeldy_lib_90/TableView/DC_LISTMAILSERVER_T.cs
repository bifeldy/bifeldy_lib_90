using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.TableView {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class DC_LISTMAILSERVER_T : JsonSerDe {
        public string MAIL_DCKODE { set; get; }
        public string MAIL_DCNAME { set; get; }
        public string MAIL_IP { set; get; }
        public string MAIL_HOSTNAME { set; get; }
        public string MAIL_PORT { set; get; }
        public string MAIL_USERNAME { set; get; }
        public string MAIL_PASSWORD { set; get; }
        public string MAIL_SENDER { set; get; }
        public DateTime? MAIL_UPDREC_DATE { set; get; }
    }

    [JsonSerializable(typeof(DC_LISTMAILSERVER_T))]
    [JsonSerializable(typeof(DC_LISTMAILSERVER_T[]))]
    [JsonSerializable(typeof(List<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(Dictionary<string, DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DC_LISTMAILSERVER_T>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DC_LISTMAILSERVER_T>))]
    public partial class DC_LISTMAILSERVER_T_JsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}