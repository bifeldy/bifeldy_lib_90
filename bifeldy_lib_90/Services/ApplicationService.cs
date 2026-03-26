using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;
using System.Reflection;

namespace bifeldy_lib_90.Services {

    public interface IApplicationService {
        bool DebugMode { get; }
        string AppName { get; }
        string AppLocation { get; }
        string AppVersion { get; }
        string GetVariabel(string key, string kunci);
        IpMacAddress[] GetIpMacAddress();
        string[] GetAllIpAddress();
        string[] GetAllMacAddress();
    }

    public sealed class CApplicationService : IApplicationService {

        private readonly ILogger<CApplicationService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _hca;
        private readonly ILockerService _locker;
        private readonly IConverterService _converter;

        private readonly Assembly _prgAsm = Assembly.GetEntryAssembly();
        // private readonly Assembly _libAsm = Assembly.GetExecutingAssembly();

        private readonly SettingLibb _SettingLibb;

        public bool DebugMode =>
#if DEBUG
                true;
#else
                false;
#endif


        public string AppName => Bifeldy.App.Environment.ApplicationName;
        public string AppLocation => AppDomain.CurrentDomain.BaseDirectory;
        public string AppVersion => this._prgAsm.GetName().Version.ToString();

        public CApplicationService(
            IOptions<EnvVar> envVar,
            ILogger<CApplicationService> logger,
            IDistributedCache cache,
            IHttpContextAccessor hca,
            ILockerService locker,
            IConverterService converter,
            IHttpService http
        ) {
            this._logger = logger;
            this._cache = cache;
            this._hca = hca;
            this._locker = locker;
            this._converter = converter;
            this._SettingLibb = new SettingLibb(envVar, http);
        }

        public string GetVariabel(string key, string kunci) {
            string cacheKey = $"{kunci}_{key}".ToLower().Trim();

            try {
                _ = this._locker.SemaphoreGlobalApp("KUNCI").Wait(-1);

                string result = this._cache.GetString(cacheKey);
                if (!string.IsNullOrEmpty(result?.Trim())) {
                    return result;
                }

                // http://xxx.xxx.xxx.xxx/KunciGxxx
                result = this._SettingLibb.GetVariabel(key, kunci);
                result = result?.Split(';').FirstOrDefault();
                result = result?.Trim();

                if (!string.IsNullOrEmpty(result)) {
                    string jsonPathKunci = Path.Combine(this.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "Kunci.json");

                    if (result.ToUpper().Contains("ERROR") || result.ToUpper().Contains("EXCEPTION") || result.ToUpper().Contains("GAGAL") || result.ToUpper().Contains("NGINX")) {
                        bool fromSavedJsonFile = false;

                        if (File.Exists(jsonPathKunci)) {
                            try {
                                string jsonContent = File.ReadAllText(jsonPathKunci);

                                var dictKunci = (IDictionary<string, object>)this._converter.JsonToObject(jsonContent);

                                if (dictKunci != null) {
                                    if (dictKunci.ContainsKey(cacheKey)) {
                                        object val = dictKunci[cacheKey];

                                        if (val != null) {
                                            result = val?.ToString();

                                            result = result?.Split(';').FirstOrDefault();
                                            result = result?.Trim();

                                            fromSavedJsonFile = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) {
                                this._logger.LogError("[KUNCI_ERR_JSON_ERR] {ex}", ex.Message);
                                File.Delete(jsonPathKunci);
                            }
                        }

                        if (!fromSavedJsonFile) {
                            if (this._hca.HttpContext != null) {
                                string reqPath = this._hca.HttpContext.Request.Path.Value;
                                if (!string.IsNullOrEmpty(reqPath)) {
                                    if (reqPath.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.OrdinalIgnoreCase)) {
                                        return null;
                                    }
                                }
                            }

                            throw new KunciServerTidakTersediaException($"Terjadi Kesalahan Saat Mendapatkan Kunci {key} @ {kunci} :: {result}");
                        }
                    }
                    else {
                        var dictKunci = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        if (File.Exists(jsonPathKunci)) {
                            string jsonContent = File.ReadAllText(jsonPathKunci);

                            try {
                                dictKunci = (Dictionary<string, object>)this._converter.JsonToObject(jsonContent);
                            }
                            catch (Exception ex) {
                                this._logger.LogError("[KUNCI_OK_JSON_ERR] {ex}", ex.Message);
                                File.Delete(jsonPathKunci);
                            }
                        }

                        if (!dictKunci.ContainsKey(cacheKey)) {
                            dictKunci.Add(cacheKey, result);
                        }

                        if (result != dictKunci[cacheKey]?.ToString()) {
                            dictKunci[cacheKey] = result;
                        }

                        File.WriteAllText(jsonPathKunci, this._converter.ObjectToJson(dictKunci));
                    }

                    if (!string.IsNullOrEmpty(result)) {
                        this._cache.SetString(cacheKey, result, new DistributedCacheEntryOptions() {
                            SlidingExpiration = TimeSpan.FromMinutes(15)
                        });
                    }
                }

                return result;
            }
            catch (Exception e) {
                this._logger.LogError("[KUNCI_ERROR] {ex}", e.Message);
                this._cache.Remove(cacheKey);
                throw;
            }
            finally {
                _ = this._locker.SemaphoreGlobalApp("KUNCI").Release();
            }
        }

        public IpMacAddress[] GetIpMacAddress() {
            var IpMacAddress = new List<IpMacAddress>();

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics) {
                if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                    string iv4 = null;
                    string iv6 = null;

                    IPInterfaceProperties ipInterface = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation ua in ipInterface.UnicastAddresses) {
                        if (!ua.Address.IsIPv4MappedToIPv6 && !ua.Address.IsIPv6LinkLocal && !ua.Address.IsIPv6Teredo && !ua.Address.IsIPv6SiteLocal) {
                            if (ua.PrefixLength <= 32) {
                                iv4 = ua.Address.ToString();
                            }
                            else if (ua.PrefixLength <= 64) {
                                iv6 = ua.Address.ToString();
                            }
                        }
                    }

                    PhysicalAddress mac = nic.GetPhysicalAddress();
                    string macAddr = mac?.ToString();

                    IpMacAddress.Add(new IpMacAddress() {
                        NAME = nic.Name,
                        DESCRIPTION = nic.Description,
                        MAC_ADDRESS = string.IsNullOrEmpty(macAddr) ? null : macAddr,
                        IP_V4_ADDRESS = iv4,
                        IP_V6_ADDRESS = iv6
                    });
                }
            }

            return [.. IpMacAddress];
        }

        public string[] GetAllIpAddress() {
            string[] iv4 = [.. this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.IP_V4_ADDRESS)).Select(d => d.IP_V4_ADDRESS.ToUpper())];
            string[] iv6 = [.. this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.IP_V6_ADDRESS)).Select(d => d.IP_V6_ADDRESS.ToUpper())];
            string[] ip = new string[iv4.Length + iv6.Length];
            iv4.CopyTo(ip, 0);
            iv6.CopyTo(ip, iv4.Length);
            return ip;
        }

        public string[] GetAllMacAddress() => [.. this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.MAC_ADDRESS)).Select(d => d.MAC_ADDRESS.ToUpper())];

    }

}