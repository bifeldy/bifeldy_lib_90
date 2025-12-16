using Microsoft.AspNetCore.Http;
using System.Text;

namespace bifeldy_lib_90.Extensions {

    public static class HttpRequestExtension {

        public static async Task<string> GetHttpRequestBodyStringAsync(this HttpRequest request, Encoding encoding = null) {
            string body = string.Empty;

            request.EnableBuffering();
            if (request.ContentLength == null || !(request.ContentLength > 0) || !request.Body.CanSeek) {
                return body;
            }

            _ = request.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8, encoding == null, 1024, true)) {
                body = await reader.ReadToEndAsync();
            }

            request.Body.Position = 0;

            return body;
        }

    }

}