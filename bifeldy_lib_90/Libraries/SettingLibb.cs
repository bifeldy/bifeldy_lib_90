using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Microsoft.Extensions.Options;

namespace bifeldy_lib_90.Libraries {

    public sealed class SettingLibb {

        private readonly EnvVar _envVar;

        private readonly IHttpService _http;

        public SettingLibb(IOptions<EnvVar> envVar, IHttpService http) {
            this._envVar = envVar.Value;
            this._http = http;
        }

        public string GetVariabel(string key, string kunciGxxx) {
            try {
                var reqBody = new KunciRequest() {
                    Key = $"mujiyono{key}"
                };

                string kunciIpDomain = this._envVar.KUNCI_IP_DOMAIN?.Trim().TrimEnd('/');
                if (string.IsNullOrEmpty(kunciIpDomain)) {
                    kunciIpDomain = "localhost";
                }

                string httpUrl = $"http://{kunciIpDomain}";
                if (!string.IsNullOrEmpty(kunciGxxx)) {
                    httpUrl += $"/{kunciGxxx}";
                }

                httpUrl += "/GetVariabel";

                Task<HttpResponseMessage> httpRes = this._http.PostData(
                    httpUrl,
                    reqBody,
                    KunciRequestJsonSerializerContext.Default.KunciRequest
                );

                HttpResponseMessage response = httpRes.Result;

                Task<string> respBody = response.Content.ReadAsStringAsync();

                return respBody.Result;
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

    }

}
