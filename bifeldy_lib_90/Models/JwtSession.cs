using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public enum ESessionRole {
        PROGRAM_SERVICE = 0,
        USER_SD_SSD_3,
        EXTERNAL_BOT
    }

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class JwtSession : JsonSerDe {
        public string name { get; set; }
        public ESessionRole role { get; set; }
    }

    [JsonSerializable(typeof(JwtSession))]
    [JsonSerializable(typeof(JwtSession[]))]
    [JsonSerializable(typeof(List<JwtSession>))]
    [JsonSerializable(typeof(Dictionary<string, JwtSession>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<JwtSession>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<JwtSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<JwtSession>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<JwtSession>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<JwtSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<JwtSession>))]
    [JsonSerializable(typeof(ResponseJsonSingle<JwtSession>))]
    [JsonSerializable(typeof(ResponseJsonMulti<JwtSession>))]
    public partial class UserSessionJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}