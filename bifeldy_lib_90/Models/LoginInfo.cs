using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class LoginInfo : JsonSerDe {
        public string user_name { get; set; }
        public string password { get; set; }
        public string secret { get; set; }

        public override string[] HiddenProperties() => ["secret"];
    }

    [JsonSerializable(typeof(LoginInfo))]
    [JsonSerializable(typeof(LoginInfo[]))]
    [JsonSerializable(typeof(List<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<LoginInfo>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<LoginInfo>))]
    [JsonSerializable(typeof(ResponseJsonSingle<LoginInfo>))]
    [JsonSerializable(typeof(ResponseJsonMulti<LoginInfo>))]
    public partial class LoginInfoJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}
