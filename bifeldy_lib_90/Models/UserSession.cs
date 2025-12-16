using bifeldy_lib_90.TableView;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public enum UserSessionRole {
        PROGRAM_SERVICE = 0,
        USER_SD_SSD_3,
        EXTERNAL_BOT
    }

    public abstract class UserSession {
        public string name { get; set; }
        public UserSessionRole role { get; set; }
    }

    public sealed class UserWebSession : UserSession {
        public string nik { get; set; }
        [JsonIgnore] public DC_USER_T dc_user_t { get; set; }
    }

    public sealed class UserApiSession : UserSession {
        // [JsonIgnore] public API_TOKEN_T dc_api_token_t { get; set; }
        // [JsonIgnore] public DC_USER_T dc_user_t { get; set; }
    }

}