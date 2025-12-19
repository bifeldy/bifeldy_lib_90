using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;

namespace bifeldy_lib_90.Endpoints {

    public static class EchoEndpoint {

        [UnconditionalSuppressMessage(
            "Trimming", "IL2026",
            Justification = "Minimal API handler is static and AOT-safe"
        )]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050",
            Justification = "Minimal API handler uses static delegate with known types"
        )]
        public static RouteGroupBuilder MapEchoEndpoints(this RouteGroupBuilder routeGroupBuilder) {
            string documentName = "latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty);

            RouteGroupBuilder apiGroup = routeGroupBuilder
                .MapGroupTagDescription("/echo", "__", "Fitur standar bawaan untuk uji coba koneksi ~")
                .WithGroupNames(documentName)
                .AllowAnonymous();

            _ = apiGroup.MapGet("/", EchoNoData).WithSummary("Echo").WithDescription("Balikin request jadi response ");
            _ = apiGroup.MapDelete("/", EchoNoData).WithSummary("Echo").WithDescription("Balikin request jadi response ");

            _ = apiGroup.MapPost("/", EchoWithData).WithSummary("Echo").WithDescription("Balikin request jadi response ");
            _ = apiGroup.MapPut("/", EchoWithData).WithSummary("Echo").WithDescription("Balikin request jadi response ");
            _ = apiGroup.MapPatch("/", EchoWithData).WithSummary("Echo").WithDescription("Balikin request jadi response ");

            return apiGroup;
        }

        private static async Task<IResult> Inspect(HttpContext http, IConverterService converter) {
            var query = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (KeyValuePair<string, StringValues> data in http.Request.Query) {
                if (data.Value.Count > 1) {
                    query.Add(data.Key, data.Value.ToArray());
                }
                else {
                    query.Add(data.Key, string.Join("; ", data.Value.ToArray()));
                }
            }

            IDictionary<string, object> headers = http.Request.Headers.Where(d => !d.Key.ToUpper().StartsWith("COOKIE")).ToDictionary(a => a.Key, a => (object)string.Join("; ", a.Value.ToArray()));
            IDictionary<string, object> cookies = http.Request.Cookies.ToDictionary(a => a.Key, a => (object)string.Join("; ", a.Value));

            object requestData = null;

            string jsonRequestBody = await http.Request.GetHttpRequestBodyStringAsync();
            if (!string.IsNullOrEmpty(jsonRequestBody)) {
                try {
                    requestData = (IDictionary<string, object>)converter.JsonToObject(jsonRequestBody);
                }
                catch {
                    requestData = jsonRequestBody;
                }
            }

            IDictionary<string, object> response = new Dictionary<string, object> {
                { "info", "200 - Echo" },
                { "method", http.Request.Method },
                { "query", query },
                { "headers", headers },
                { "cookies", cookies },
                { "body", requestData }
            };

            string jsonResponse = converter.ObjectToJson(response);
            return Results.Text(jsonResponse, MediaTypeNames.Application.Json);
        }

        private static Task<IResult> EchoNoData(HttpContext http, IConverterService converter) {
            return Inspect(http, converter);
        }

        private static Task<IResult> EchoWithData(HttpContext http, IConverterService converter) {
            return Inspect(http, converter);
        }

    }

}
