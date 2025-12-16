using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Services {

    public interface IHttpService {
        List<Tuple<string, string>> CleanHeader(IHeaderDictionary httpHeader);
        HttpClient CreateHttpClient(uint timeoutSeconds = 60, string publicKeysBase64HashJsonFilePath = null);
        Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null);
        IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default) where T : JsonSerDe;
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> PostData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe;
        Task<HttpResponseMessage> PutData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe;
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> PatchData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe;
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
    }

    public sealed class CHttpService : IHttpService {

        private readonly ILogger<CHttpService> _logger;
        private readonly IConverterService _cs;

        private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase) {
            // RFC 7230 / RFC 9110
            "Connection",
            "Proxy-Connection",
            "Keep-Alive",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
    
            // Wildcards (never forward)
            "Proxy-*",
            "Sec-*"
        };


        private static readonly HashSet<string> RequestHeadersToRemove = new(StringComparer.OrdinalIgnoreCase) {
            "Host",               // replaced by HttpClient
            "Content-Length",     // recalculated by HttpClient
            "Content-Encoding",   // ASP.NET may already decompress
            "Transfer-Encoding",  // HttpClient will decide
            "Expect"              // avoid 100-continue problems

            /* "x-real-ip", "cf-connecting-ip", */

            // Optional: depends on your proxy policy
            // "Accept-Encoding", // do not forward if proxy wants to decompress
            // "User-Agent",
            // "Forwarded", "X-Forwarded-*"
        };

        private static readonly HashSet<string> ResponseHeadersToRemove = new(StringComparer.OrdinalIgnoreCase) {
            "Transfer-Encoding",
            "Content-Length",
            "Connection",
            "Keep-Alive",
            "Proxy-Connection",
            "Trailer"
        };

        public CHttpService(ILogger<CHttpService> logger, IConverterService cs) {
            this._logger = logger;
            this._cs = cs;
        }

        public List<Tuple<string, string>> CleanHeader(IHeaderDictionary headers) {
            var list = new List<Tuple<string, string>>();

            foreach (KeyValuePair<string, StringValues> header in headers) {
                if (RequestHeadersToRemove.Contains(header.Key)) {
                    continue;
                }

                if (HopByHopHeaders.Any(p => HeaderMatches(p, header.Key))) {
                    continue;
                }

                list.Add(Tuple.Create(header.Key, header.Value.ToString()));
            }

            return list;
        }

        private HttpContent CreateStreamingJsonContent<T>(IAsyncEnumerable<T> stream, JsonTypeInfo<T> typeInfo) where T : JsonSerDe {
            return new AsyncEnumerableJsonContent<T>(stream, "application/json", typeInfo, false);
        }

        private HttpContent CreateStreamingNdjsonContent<T>(IAsyncEnumerable<T> stream, JsonTypeInfo<T> typeInfo) where T : JsonSerDe {
            return new AsyncEnumerableJsonContent<T>(stream, "application/x-ndjson", typeInfo, true);
        }

        private HttpContent GetHttpContentJson<T>(T payload, JsonTypeInfo<T> jsonTypeInfo, string contentType, Encoding encoding = null) where T : JsonSerDe {
            encoding ??= Encoding.UTF8;

            if (payload is IAsyncEnumerable<T> asyncEnumerable) {
                string ct = contentType?.ToLowerInvariant();

                if (ct == "application/json") {
                    return this.CreateStreamingJsonContent(asyncEnumerable, jsonTypeInfo);
                }

                if (ct == "application/x-ndjson") {
                    return this.CreateStreamingNdjsonContent(asyncEnumerable, jsonTypeInfo);
                }

                throw new Exception($"Streaming Untuk Content-Type '{contentType}' Tidak Tersedia");
            }

            string json = this._cs.ObjectToJson(payload, jsonTypeInfo);
            return new StringContent(json, encoding, contentType);
        }

        private HttpContent GetHttpContent(object payload, string contentType, Encoding encoding = null) {
            encoding ??= Encoding.UTF8;

            if (payload is string s) {
                return new StringContent(s, encoding, contentType);
            }

            if (payload is byte[] b) {
                return new ByteArrayContent(b);
            }

            if (payload is HttpRequest req) {
                var streamContent = new StreamContent(req.Body);

                if (!string.IsNullOrEmpty(req.ContentType)) {
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(req.ContentType);
                }

                streamContent.Headers.ContentLength = null;
                streamContent.Headers.ContentEncoding.Clear();

                return streamContent;
            }

            if (payload is Stream stream) {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return streamContent;
            }

            throw new InvalidOperationException($"Payload type '{payload.GetType()}' is not supported as raw HTTP content");
        }

        private HttpRequestMessage ParseApiDataJson<T>(
            string httpUri, HttpMethod httpMethod, T payload, JsonTypeInfo<T> jsonTypeInfo,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null
        ) where T : JsonSerDe {
            encoding ??= Encoding.UTF8;

            var request = new HttpRequestMessage {
                Method = httpMethod,
                RequestUri = new Uri(httpUri)
            };

            if (payload != null) {
                HttpContent finalContent = null;

                if (multipart) {
                    var form = new MultipartFormDataContent();

                    if (payload is T[] arr) {
                        for (int i = 0; i < arr.Length; i++) {
                            HttpContent part = this.GetHttpContentJson(
                                (T)arr.GetValue(i),
                                jsonTypeInfo,
                                contentType?.ElementAtOrDefault(i) ?? "application/octet-stream",
                                encoding
                            );

                            form.Add(
                                part,
                                contentKeyName?.ElementAtOrDefault(i) ?? "file"
                            );
                        }
                    }
                    else {
                        form.Add(
                            this.GetHttpContentJson(
                                payload,
                                jsonTypeInfo,
                                contentType?.FirstOrDefault() ?? "application/octet-stream",
                                encoding
                            ),
                            contentKeyName?.FirstOrDefault() ?? "file"
                        );
                    }

                    finalContent = form;
                }
                else {
                    finalContent = this.GetHttpContentJson(
                        payload,
                        jsonTypeInfo,
                        contentType?.FirstOrDefault() ?? "application/json",
                        encoding
                    );
                }

                request.Content = finalContent;
            }

            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    if (!request.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2)) {
                        _ = (request.Content?.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2));
                    }
                }
            }

            return request;
        }

        private HttpRequestMessage ParseApiData(
            string httpUri, HttpMethod httpMethod, object payload,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null
        ) {
            var request = new HttpRequestMessage {
                Method = httpMethod,
                RequestUri = new Uri(httpUri)
            };

            if (payload != null) {
                HttpContent finalContent = null;

                if (multipart) {
                    var form = new MultipartFormDataContent();

                    if (payload is object[] arr) {
                        for (int i = 0; i < arr.Length; i++) {
                            HttpContent part = this.GetHttpContent(
                                arr.GetValue(i),
                                contentType?.ElementAtOrDefault(i) ?? "application/octet-stream",
                                encoding
                            );

                            form.Add(
                                part,
                                contentKeyName?.ElementAtOrDefault(i) ?? "file"
                            );
                        }
                    }
                    else {
                        form.Add(
                            this.GetHttpContent(
                                payload,
                                contentType?.FirstOrDefault() ?? "application/octet-stream",
                                encoding
                            ),
                            contentKeyName?.FirstOrDefault() ?? "file"
                        );
                    }

                    finalContent = form;
                }
                else {
                    finalContent = this.GetHttpContent(
                        payload,
                        contentType?.FirstOrDefault() ?? "application/json",
                        encoding
                    );
                }

                request.Content = finalContent;
            }

            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    if (!request.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2)) {
                        _ = (request.Content?.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2));
                    }
                }
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithRetryJson<T>(
            string httpUri, HttpMethod httpMethod, T httpContent, JsonTypeInfo<T> jsonTypeInfo,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null,
            uint timeoutSeconds = 180, uint maxRetry = 3,
            HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead,
            string publicKeysBase64HashJsonFilePath = null
        ) where T : JsonSerDe {
            HttpClient httpClient = this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath);

            HttpResponseMessage httpResponseMessage = null;

            for (int retry = 0; retry < maxRetry; retry++) {
                using (HttpRequestMessage httpRequestMessage = this.ParseApiDataJson(
                    httpUri, httpMethod, httpContent, jsonTypeInfo,
                    multipart, httpHeaders,
                    contentKeyName, contentType,
                    encoding ?? Encoding.UTF8
                )) {
                    httpRequestMessage.Headers.Add("x-retry-number", $"{retry}");

                    try {
                        httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, readOpt);

                        if (((int)httpResponseMessage.StatusCode) < 500) {
                            break;
                        }
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[HTTP_REQUEST_{method}] {ex}", httpRequestMessage.Method.Method, ex.Message);
                    }
                    finally {
                        await Task.Delay(Math.Min((int)timeoutSeconds / (int)maxRetry * retry, 5 * retry) * 1000);
                    }
                }
            }

            return httpResponseMessage;
        }

        private async Task<HttpResponseMessage> SendWithRetry(
            string httpUri, HttpMethod httpMethod, object httpContent,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null,
            uint timeoutSeconds = 180, uint maxRetry = 3,
            HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead,
            string publicKeysBase64HashJsonFilePath = null
        ) {
            HttpClient httpClient = this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath);

            HttpResponseMessage httpResponseMessage = null;

            for (int retry = 0; retry < maxRetry; retry++) {
                using (HttpRequestMessage httpRequestMessage = this.ParseApiData(
                    httpUri, httpMethod, httpContent,
                    multipart, httpHeaders,
                    contentKeyName, contentType,
                    encoding ?? Encoding.UTF8
                )) {
                    httpRequestMessage.Headers.Add("x-retry-number", $"{retry}");

                    try {
                        httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, readOpt);

                        if (((int)httpResponseMessage.StatusCode) < 500) {
                            break;
                        }
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[HTTP_REQUEST_{method}] {ex}", httpRequestMessage.Method.Method, ex.Message);
                    }
                    finally {
                        await Task.Delay(Math.Min((int)timeoutSeconds / (int)maxRetry * retry, 5 * retry) * 1000);
                    }
                }
            }

            return httpResponseMessage;
        }

        public HttpClient CreateHttpClient(uint timeoutSeconds = 60, string publicKeysBase64HashJsonFilePath = null) {
            var httpMessageHandler = new HttpClientHandler() {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            if (!string.IsNullOrEmpty(publicKeysBase64HashJsonFilePath)) {
                string json = File.ReadAllText(publicKeysBase64HashJsonFilePath);

                var lsJson = (List<string>)this._cs.JsonToObject(json);
                var pinnedPublicKeys = new HashSet<string>(lsJson, StringComparer.OrdinalIgnoreCase);

                httpMessageHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, x509Certificate2, x509Chain, sslPolicyErrors) => {
                    if (sslPolicyErrors == SslPolicyErrors.None) {
                        byte[] serverPublicKey = x509Certificate2.GetPublicKey();

                        using (var sha256 = SHA256.Create()) {
                            byte[] hash = sha256.ComputeHash(serverPublicKey);
                            string base64Hash = Convert.ToBase64String(hash);

                            if (pinnedPublicKeys.Contains(base64Hash)) {
                                return true;
                            }
                        }

                        foreach (X509ChainElement element in x509Chain.ChainElements) {
                            byte[] chainServerPublicKey = element.Certificate.GetPublicKey();

                            using (var sha256 = SHA256.Create()) {
                                byte[] hash = sha256.ComputeHash(chainServerPublicKey);
                                string base64Hash = Convert.ToBase64String(hash);

                                if (pinnedPublicKeys.Contains(base64Hash)) {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                };
            }

            return new HttpClient(httpMessageHandler) {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        private static bool HeaderMatches(string pattern, string header) {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(header)) {
                return false;
            }

            pattern = pattern.ToLowerInvariant();
            header = header.ToLowerInvariant();

            if (pattern.EndsWith("*")) {
                string prefix = pattern[..^1];
                return header.StartsWith(prefix);
            }

            return header == pattern;
        }

        public async Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null) {
            List<Tuple<string, string>> lsHeader = this.CleanHeader(request.Headers);

            HttpRequestMessage forwardMsg = this.ParseApiData(
                urlTarget,
                new HttpMethod(request.Method),
                request,
                httpHeaders: lsHeader,
                contentType: request.ContentType != null ? [request.ContentType] : null
            );

            HttpResponseMessage res = await this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath)
                .SendAsync(forwardMsg, HttpCompletionOption.ResponseHeadersRead);

            int statusCode = (int)res.StatusCode;

            response.Clear();
            response.StatusCode = statusCode;

            if (statusCode == 404 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                return new NotFoundObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "404 - Whoops :: Alamat Server Tujuan Tidak Ditemukan",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Periksa Kembali Dokumentasi API"
                    }
                });
            }
            else if (statusCode == 502 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                return new BadRequestObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "502 - Whoops :: Alamat Server Tujuan Tidak Tersedia",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Hubungi S/SD 3 Untuk informasi Lebih Lanjut"
                    }
                });
            }

            KeyValuePair<string, IEnumerable<string>>[] allHeaders = [.. res.Headers, .. res.Content.Headers];
            string[] blockedHeaders = [.. HopByHopHeaders.Union(ResponseHeadersToRemove)];

            foreach (KeyValuePair<string, IEnumerable<string>> header in allHeaders) {
                if (!blockedHeaders.Any(b => HeaderMatches(b, header.Key))) {
                    response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            await response.StartAsync();

            using (Stream upstream = await res.Content.ReadAsStreamAsync()) {
                byte[] buffer = new byte[8192];
                int bytesRead = 0;

                while ((bytesRead = await upstream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await response.Body.WriteAsync(buffer, 0, bytesRead);
                    await response.Body.FlushAsync();
                }
            }

            return new EmptyResult();
        }

        public async IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : JsonSerDe {
            if (response == null) {
                throw new Exception("Response Tidak Ada Isinya");
            }

            if (!response.IsSuccessStatusCode) {
                throw new Exception($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            Stream stream = await response.Content.ReadAsStreamAsync();

            if (response.Content.Headers.ContentType?.MediaType == "application/json") {
                await foreach (T item in JsonSerializer.DeserializeAsyncEnumerable(stream, jsonTypeInfo, cancellationToken)) {
                    if (item != null) {
                        yield return item;
                    }
                }

                yield break;
            }

            if (response.Content.Headers.ContentType?.MediaType == "application/x-ndjson") {
                using (var reader = new StreamReader(stream)) {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested) {
                        string line = await reader.ReadLineAsync();

                        if (!string.IsNullOrWhiteSpace(line)) {
                            T item = default;

                            try {
                                item = JsonSerializer.Deserialize(line, jsonTypeInfo);
                            }
                            catch {
                                throw new Exception("Format X-(ND)JSON Harus Per Baris 1 Object Lengkap");
                            }

                            if (item != null) {
                                yield return item;
                            }
                        }
                    }
                }

                yield break;
            }

            throw new Exception($"Streaming Untuk Content-Type '{response.Content.Headers.ContentType?.MediaType}' Tidak Tersedia");
        }

        public async Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Head, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Get, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Delete, null,httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PostData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe {
            return await this.SendWithRetryJson(urlPath, HttpMethod.Post, objBody, jsonTypeInfo, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PutData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe {
            return await this.SendWithRetryJson(urlPath, HttpMethod.Put, objBody, jsonTypeInfo, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("CONNECT"), null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("OPTIONS"), null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PatchData<T>(string urlPath, T objBody, JsonTypeInfo<T> jsonTypeInfo, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) where T : JsonSerDe {
            return await this.SendWithRetryJson(urlPath, new HttpMethod("PATCH"), objBody, jsonTypeInfo, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Trace, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

    }

}
