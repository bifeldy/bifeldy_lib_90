using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.TableView;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public enum UserSessionRole {
        PROGRAM_SERVICE = 0,
        USER_SD_SSD_3,
        EXTERNAL_BOT
    }

    public abstract class UserSession : JsonSerDe {
        public string name { get; set; }
        public UserSessionRole role { get; set; }
    }

    public sealed class UserWebSession : UserSession {
        public string nik { get; set; }
        [JsonIgnore] public DC_USER_T dc_user_t { get; set; }

        public override string[] HiddenProperties() => ["dc_user_t"];
    }

    public sealed class UserApiSession : UserSession {
        // [JsonIgnore] public API_TOKEN_T dc_api_token_t { get; set; }
        // [JsonIgnore] public DC_USER_T dc_user_t { get; set; }
    }

    [JsonSerializable(typeof(UserWebSession))]
    [JsonSerializable(typeof(UserWebSession[]))]
    [JsonSerializable(typeof(List<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<UserWebSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<UserWebSession>))]
    [JsonSerializable(typeof(ResponseJsonSingle<UserWebSession>))]
    [JsonSerializable(typeof(ResponseJsonMulti<UserWebSession>))]
    [JsonSerializable(typeof(UserApiSession))]
    [JsonSerializable(typeof(UserApiSession[]))]
    [JsonSerializable(typeof(List<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<UserApiSession>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<UserApiSession>))]
    [JsonSerializable(typeof(ResponseJsonSingle<UserApiSession>))]
    [JsonSerializable(typeof(ResponseJsonMulti<UserApiSession>))]
    public partial class UserSessionJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}