using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Endpoints;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Middlewares;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.Transformers;
using Helmet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace bifeldy_lib_90 {

    public static class Bifeldy {

        public const string DEFAULT_ASSETS_FOLDER = "_assets";
        public const string DEFAULT_DATA_FOLDER = "_data";

        public static DateTime? LAST_GC_RUN = null;

        public static bool IS_USING_SECRET = false;
        public static bool IS_USING_API_KEY = false;
        public static bool IS_USING_JWT = false;

        public static string API_PREFIX = null;
        public static string NGINX_PATH_NAME = "x-forwarded-prefix";

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;

        public static IConfiguration Config = null;

        public static WebApplication App = null;

        public static void AppContextOverride() {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            // AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public static void InitBuilder(WebApplicationBuilder builder) {
            Builder = builder;
            Services = builder.Services;
            Config = builder.Configuration;
        }

        public static void SetInvariantCulture() {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            _ = Services.Configure<RequestLocalizationOptions>(options => {
                var supportedCultures = new List<CultureInfo>() {
                    CultureInfo.InvariantCulture,
                    // new("en-US"),
                    // new("id-ID")
                };

                options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);

                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.ApplyCurrentCultureToResponseHeaders = true;
            });
        }

        public static void ConfigureFormDataBody() {
            _ = Services.Configure<JsonOptions>(opt => {
                opt.SerializerOptions.Converters.Add(new DecimalConverter());
                opt.SerializerOptions.Converters.Add(new NullableDecimalConverter());
            });
            _ = Services.Configure<FormOptions>(o => {
                o.MultipartBodyLengthLimit = long.MaxValue;
            });
        }

        public static void SetKestrelPort(IConfigurationManager configurationManager = null) {
            IConfigurationManager config = configurationManager ?? Builder.Configuration;

            // Web Api Seperti Biasa
            string apiEnvName = "API_PORT";
            string apiPortEnv = Environment.GetEnvironmentVariable(apiEnvName);
            int webApiPort = int.Parse(apiPortEnv ?? config[$"ENV:{apiEnvName}"]);

            string logInfo = $"=> Running Port :: {webApiPort} (API)";

            Console.WriteLine(logInfo);

            _ = Builder.WebHost.ConfigureKestrel(options => {
                options.Limits.MaxRequestBodySize = long.MaxValue;

                options.Listen(IPAddress.Any, webApiPort, listenOptions => {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
            });
        }

        public static void SetupSerilog() {
            _ = Services.AddSingleton<SerilogKunciGxxxPropertyEnricher>();
            _ = Builder.Host.UseSerilog((hostContext, services, configuration) => {
                string appPathDir = AppDomain.CurrentDomain.BaseDirectory;
                SerilogKunciGxxxPropertyEnricher spe = services.GetRequiredService<SerilogKunciGxxxPropertyEnricher>();
                _ = configuration.Enrich.With(spe).WriteTo.File(
                    appPathDir + $"/{DEFAULT_DATA_FOLDER}/logs/error_.txt",
                    LogEventLevel.Error,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {KunciGxxx} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                );
            });
        }

        public static void UseSerilog() {
            _ = App.UseSerilogRequestLogging(o => {
                o.MessageTemplate = "{TraceId} :: {RemoteOriginIpAddress} :: {RequestMethod} :: {RequestPath} :: {StatusCode} :: {Elapsed:0.0000} ms";
                // o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Error;
                o.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    diagnosticContext.Set("TraceId", Activity.Current?.Id ?? httpContext?.TraceIdentifier);
                    diagnosticContext.Set("RemoteOriginIpAddress", httpContext?.Items["ip_origin"]);
                };
            });
        }

        public static void AddOpenApi(
            string title = "Open API",
            string description = "Documentation ~",
            bool enableApiKey = true,
            bool enableJwt = false,
            string apiPrefix = "api",
            string[] documents = null
        ) {
            if (string.IsNullOrWhiteSpace(apiPrefix)) {
                throw new Exception("API Prefix Wajib Di Isi");
            }

            API_PREFIX ??= apiPrefix;

            List<string> docs = ["latest-" + Assembly.GetEntryAssembly().GetName().Version ?.ToString().Replace(".", string.Empty)];
            if (documents != null) {
                foreach (string document in documents) {
                    if (!docs.Contains(document)) {
                        docs.Add(document);
                    }
                }
            }

            _ = Services.AddSingleton(new DocumentOptions(title, description, enableApiKey, enableJwt));

            foreach (string documentName in docs) {
                _ = Services.AddOpenApi(documentName, options => {
                    _ = options.AddDocumentTransformer<DocumentTransformer>();
                    _ = options.AddSchemaTransformer<IgnorePropertySchemaTransformer>();
                });
            }
        }

        public static void MapOpenApi(string jsonFileName = "openapi", string[] documents = null) {
            if (string.IsNullOrWhiteSpace(jsonFileName)) {
                throw new Exception("Json File Name Wajib Di Isi");
            }

            string jsonFilePath = $"/{jsonFileName}" + "-{documentName}.json";
            _ = App.MapOpenApi(jsonFilePath);

            List<string> docs = ["latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty)];
            if (documents != null) {
                foreach (string document in documents) {
                    if (!docs.Contains(document)) {
                        docs.Add(document);
                    }
                }
            }

            _ = App.MapScalarApiReference(API_PREFIX, opt => {
                _ = opt.WithOpenApiRoutePattern(jsonFilePath);
                _ = opt.WithTheme(ScalarTheme.DeepSpace);
                _ = opt.HideModels();
                _ = opt.ExpandAllTags();
                _ = opt.AddDocuments(docs);
            });
        }

        public static void AddRedisDistributedCache(IConfigurationManager configurationManager = null) {
            IConfigurationManager config = configurationManager ?? Builder.Configuration;

            string redisEnvName = "REDIS";
            string redisEnvVal = Environment.GetEnvironmentVariable(redisEnvName);
            string redisConstStr = redisEnvVal ?? config[$"ENV:{redisEnvName}"];

            if (string.IsNullOrEmpty(redisConstStr)) {
                _ = Services.AddDistributedMemoryCache(options => {
                    // No Additional Config ~
                });
            }
            else {
                _ = Services.AddStackExchangeRedisCache(options => {
                    options.Configuration = redisConstStr;
                    options.InstanceName = $"{App.Environment.ApplicationName}_Cache";
                });
            }
        }

        public static void AddDependencyInjection() {
            _ = Services.AddHttpContextAccessor();

            // --
            // Transient Selalu Dapat Object Baru ~
            // --
            _ = Services.AddTransient<ISqlite, CSqlite>();
            _ = Services.AddTransient<IPostgres, CPostgres>();
            _ = Services.AddTransient<IMsSQL, CMsSQL>();

            // --
            // Hanya Singleton Yang Leluasa Dengan Mudahnya Bisa Di Inject Di Constructor() { } Dimana Saja
            // --
            _ = Services.AddSingleton<IApplicationService, CApplicationService>();
            _ = Services.AddSingleton<IChiperService, CChiperService>();
            _ = Services.AddSingleton<IConverterService, CConverterService>();
            _ = Services.AddSingleton<IGlobalService, CGlobalService>();
            _ = Services.AddSingleton<IHttpService, CHttpService>();
            _ = Services.AddSingleton<ILockerService, CLockerService>();
            _ = Services.AddSingleton<IStreamService, CStreamService>();

            // --
            // Transient Selalu Dapat Object Baru ~
            // --
            _ = Services.AddScoped<IApiKeyRepository, CApiKeyRepository>();
            _ = Services.AddScoped<IApiTokenRepository, CApiTokenRepository>();
            _ = Services.AddScoped<IServerConfigRepository, CServerConfigRepository>();
            _ = Services.AddScoped<IUserRepository, CUserRepository>();
        }

        public static void InitApp(WebApplication app, bool forceGcToCleanUpRamEveryRequest = false, int gcDelaySkipRunMinutes = 30) {
            App = app;

            if (forceGcToCleanUpRamEveryRequest) {
                _ = App.Use(async (context, next) => {
                    context.Response.OnCompleted(() => {
                        if (LAST_GC_RUN != null && (DateTime.Now - LAST_GC_RUN.Value).TotalMinutes < gcDelaySkipRunMinutes) {
                            return Task.CompletedTask;
                        }

                        LAST_GC_RUN = DateTime.Now;

                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
                        GC.WaitForPendingFinalizers();
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, false);

                        return Task.CompletedTask;
                    });

                    await next();
                });
            }
        }

        public static void UseForwardedHeaders() {
            _ = App.UseForwardedHeaders(
                new ForwardedHeadersOptions() {
                    ForwardedHeaders = ForwardedHeaders.All
                }
            );
        }

        public static void UseHelmet() {
            _ = App.UseHelmet(o => {
                o.UseContentSecurityPolicy = false; // Buat Web Socket (Blazor SignalR, Socket.io, Web RTC)
                o.UseXContentTypeOptions = false; // Boleh Content-Sniff :: .mkv Dibaca .mp4
                o.UseReferrerPolicy = false; // Kalau Pakai Service Worker (Gak Set Origin, Tapi Referrer)
            });
        }

        public static void UseNginxProxyPathSegment() {
            _ = App.Use((context, next) => {
                if (context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues values)) {
                    string prefix = values.Last()?.TrimEnd('/');

                    if (!string.IsNullOrEmpty(prefix)) {
                        if (string.IsNullOrEmpty(context.Request.PathBase)) {
                            context.Request.PathBase = prefix;
                        }

                        if (context.Request.Path.StartsWithSegments(prefix, out PathString remaining)) {
                            context.Request.Path = remaining;
                        }
                    }
                }

                return next();
            });
        }

        public static void AutoCheckMultiDc() {
            string appLocation = AppDomain.CurrentDomain.BaseDirectory;

            AssemblyName prgAsm = Assembly.GetEntryAssembly().GetName();
            AssemblyName libAsm = Assembly.GetExecutingAssembly().GetName();

            string dataFolderPath = Path.Combine(appLocation, DEFAULT_DATA_FOLDER);
            _ = Directory.CreateDirectory(dataFolderPath);

            string targetDatabaseLocationApp = Path.Combine(dataFolderPath, $"{prgAsm.Name}.db");

            if (!File.Exists(targetDatabaseLocationApp)) {
                string defaultDatabaseLocation = Path.Combine(appLocation, $"{prgAsm.Name}.db");

                if (!File.Exists(defaultDatabaseLocation)) {
                    string targetDatabaseLocationLib = Path.Combine(dataFolderPath, $"{libAsm.Name}.db");

                    if (!File.Exists(targetDatabaseLocationLib)) {
                        defaultDatabaseLocation = Path.Combine(appLocation, $"{libAsm.Name}.db");

                        if (!File.Exists(defaultDatabaseLocation)) {
                            throw new FileNotFoundException("Default Database Not Found!", defaultDatabaseLocation);
                        }
                    }
                }

                if (!File.Exists(targetDatabaseLocationApp)) {
                    File.Copy(defaultDatabaseLocation, targetDatabaseLocationApp);
                }
            }

            _ = App.UseMiddleware<AutoCheckMultiDcMiddleware>();
        }

        public static void UseRequestVariableInitializerMiddleware() {
            _ = App.UseMiddleware<RequestVariableInitializerMiddleware>();
        }

        public static void UseSecretMiddleware() {
            _ = App.UseMiddleware<SecretMiddleware>();
            IS_USING_SECRET = true;
        }

        public static void UseApiKeyMiddleware() {
            _ = App.UseMiddleware<ApiKeyMiddleware>();
            IS_USING_API_KEY = true;
        }

        public static void UseJwtMiddleware() {
            _ = App.UseMiddleware<JwtMiddleware>();
            IS_USING_JWT = true;
        }

        public static void Handle500ApiError<T>(string apiPrefix = "api") {
            if (string.IsNullOrWhiteSpace(apiPrefix)) {
                throw new Exception("API Prefix Wajib Di Isi");
            }

            API_PREFIX ??= apiPrefix;

            _ = App.Use(async (context, next) => {

                // Khusus API Path :: Akan Di Handle Error Dengan Balikan Data JSON
                // Selain Itu Atau Jika Masih Ada Error Lain
                // Misal Di Catch Akan Terlempar Ke Halaman Error Bawaan UI

                if (!context.Request.Path.Value.StartsWith($"/{API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase)) {
                    await next();
                }
                else {
                    try {
                        await next();
                    }
                    catch (Exception ex) {
                        ILogger<T> _logger = context.RequestServices.GetRequiredService<ILogger<T>>();

                        var user = (JwtSession)context.Items["user"];

                        HttpRequest request = context.Request;
                        HttpResponse response = context.Response;

                        response.Clear();

                        string xRequestTraceProxy = null;
                        if (response.Headers.ContainsKey("x-request-trace-proxy")) {
                            xRequestTraceProxy = response.Headers["x-request-trace-proxy"];
                        }
                        else {
                            if (request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                                string proxyPath = pathBase.Last();
                                if (!string.IsNullOrEmpty(proxyPath)) {
                                    xRequestTraceProxy = proxyPath;
                                    response.Headers.Append("x-request-trace-proxy", xRequestTraceProxy);
                                }
                            }
                        }

                        string xRequestTraceActivity = null;
                        if (response.Headers.ContainsKey("x-request-trace-activity")) {
                            xRequestTraceActivity = response.Headers["x-request-trace-activity"];
                        }
                        else {
                            xRequestTraceActivity = Activity.Current?.Id;
                            response.Headers.Append("x-request-trace-activity", xRequestTraceActivity);
                        }

                        string xRequestTraceId = null;
                        if (response.Headers.ContainsKey("x-request-trace-id")) {
                            xRequestTraceId = response.Headers["x-request-trace-id"];
                        }
                        else {
                            xRequestTraceId = context?.TraceIdentifier;
                            response.Headers.Append("x-request-trace-id", xRequestTraceId);
                        }

                        response.StatusCode = StatusCodes.Status500InternalServerError;

                        string errMsg = ex.Message;

                        Exception ie = ex.InnerException;
                        while (ie != null) {
                            errMsg += " ~ " + ie.Message;
                            ie = ie.InnerException;
                        }

                        string errDtl = errMsg + Environment.NewLine + ex.StackTrace;

                        _logger.LogError(
                            "[ERROR_HANDLER] {TraceId} {xRequestTraceProxy} 💣 {Message}",
                            xRequestTraceActivity, xRequestTraceProxy, errDtl
                        );

                        context.Items["error_detail"] = errDtl;

                        bool showErrorDetail = App.Environment.IsDevelopment() || user?.role <= ESessionRole.USER_SD_SSD_3;
                        await response.WriteAsJsonAsync(
                            new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = $"{response.StatusCode} - Whoops :: Terjadi Kesalahan",
                                result = new ResponseJsonMessage() {
                                    message = showErrorDetail ? errDtl : "Gagal Melanjutkan Permintaan"
                                }
                            },
                            ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                        );
                    }
                }
            });
        }

        public static RouteGroupBuilder StartApiWithPrefix(string apiPrefix = "api", bool redirectIndexToApi = true) {
            if (string.IsNullOrWhiteSpace(apiPrefix)) {
                throw new Exception("API Prefix Wajib Di Isi");
            }

            API_PREFIX ??= apiPrefix;

            _ = App.Use(async (context, next) => {
                await next();

                if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted) {
                    await context.Response.WriteAsJsonAsync(
                        new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "404 - Whoops :: API Tidak Ditemukan",
                            result = new ResponseJsonMessage() {
                                message = $"Silahkan Periksa Kembali Dokumentasi API"
                            }
                        },
                        ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                    );
                }
            });

            if (redirectIndexToApi) {
                _ = App.Map("/", async (context) => {
                    context.Response.Redirect($"/{API_PREFIX}", true, true);
                    await Task.CompletedTask;
                });
            }

            // TODO: Add additional MapEndpoints here
            RouteGroupBuilder routeGroup = App.MapGroup($"/{API_PREFIX}");
            _ = routeGroup.MapDefaultEndpoints();

            return routeGroup;
        }

    }

}
