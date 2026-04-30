using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Handlers;
using bifeldy_lib_90.JobSchedulers;
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace bifeldy_lib_90 {

    public static class Bifeldy {

        public const string DEFAULT_ASSETS_FOLDER = "_assets";
        public const string DEFAULT_DATA_FOLDER = "_data";

        public static DateTime? GC_RUN_LAST_DATE = null;
        public static int GC_RUN_INTERVAL = 30;

        public static string API_PREFIX = null;
        public static string NGINX_PATH_NAME = "X-Forwarded-Prefix";

        public static List<string> OPEN_API_DOCUMENTS = ApiDocumentName.ApiDefaultDocuments;

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;

        public static IConfiguration Config = null;

        public static WebApplication App = null;

        private static readonly ConcurrentDictionary<string, ScheduleBuilder> Schedules = new();

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
            _ = Services.AddHttpContextAccessor();
            _ = Services.AddSingleton<SerilogKunciGxxxPropertyEnricher>();
            _ = Builder.Host.UseSerilog((hostContext, services, configuration) => {
                SerilogKunciGxxxPropertyEnricher enricher = services.GetRequiredService<SerilogKunciGxxxPropertyEnricher>();

                _ = configuration
                    .Enrich.FromLogContext()
                    .Enrich.With(enricher)
                    .WriteTo.File(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, "logs", "error_.txt"),
                        restrictedToMinimumLevel: LogEventLevel.Error,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {KunciGxxx} | {Message:lj}{NewLine}{Exception}",
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
            string apiTitle,
            string apiDescription,
            string apiPrefix = "api",
            bool enableApiKey = true,
            bool enableJwt = false,
            params string[] documents
        ) {
            if (string.IsNullOrWhiteSpace(apiPrefix)) {
                throw new Exception("API Prefix Wajib Di Isi");
            }

            API_PREFIX ??= apiPrefix;

            documents ??= [.. ApiDocumentName.ApiDefaultDocuments];
            foreach (string document in documents) {
                string doc = Regex.Replace(document, "[^a-zA-Z0-9_-]+", string.Empty);

                if (!OPEN_API_DOCUMENTS.Contains(doc)) {
                    OPEN_API_DOCUMENTS.Add(doc);
                }
            }

            if (!OPEN_API_DOCUMENTS.Contains(ApiDocumentName._ALL_)) {
                OPEN_API_DOCUMENTS.Add(ApiDocumentName._ALL_);
            }

            _ = Services.AddSingleton(new DocumentOptions(apiTitle, apiDescription, enableApiKey, enableJwt));

            foreach (string documentName in OPEN_API_DOCUMENTS) {
                _ = Services.AddOpenApi(documentName, options => {
                    _ = options.AddDocumentTransformer<DocumentTransformer>();
                    _ = options.AddSchemaTransformer<IgnorePropertySchemaTransformer>();
                });
            }
        }

        public static void MapOpenApi(string jsonFileName = "openapi") {
            if (string.IsNullOrWhiteSpace(jsonFileName)) {
                throw new Exception("Json File Name Wajib Di Isi");
            }

            string jsonFilePath = $"/{jsonFileName}" + "-{documentName}.json";
            _ = App.MapOpenApi(jsonFilePath);

            _ = App.MapGet($"/{API_PREFIX}", async context => {
                IApplicationService app = context.RequestServices.GetRequiredService<IApplicationService>();

                string redirectUrl = "/docs";

                if (!app.DebugMode && context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                    string proxyPath = pathBase.Last();
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        redirectUrl = $"{proxyPath}{redirectUrl}";
                    }
                }

                context.Response.Redirect(redirectUrl, true, true);
                await Task.CompletedTask;
            });

            _ = App.MapScalarApiReference("/docs", opt => {
                _ = opt.WithOpenApiRoutePattern(jsonFilePath);
                _ = opt.WithTheme(ScalarTheme.DeepSpace);
                _ = opt.WithClassicLayout();
                _ = opt.HideModels();
                _ = opt.ExpandAllTags();
                _ = opt.AddDocuments(OPEN_API_DOCUMENTS);
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
            _ = Services.AddProblemDetails();
            _ = Services.AddSingleton<IDataSourceCache, CDataSourceCache>();

            // --
            // Transient Selalu Dapat Object Baru ~
            // --
            _ = Services.AddTransient<ISqlite, CSqlite>();
            _ = Services.AddTransient<IPostgres, CPostgres>();
            _ = Services.AddTransient<IMsSQL, CMsSQL>();

            // --
            // Hanya Singleton Yang Leluasa Dengan Mudahnya Bisa Di Inject Di Constructor() { } Dimana Saja
            // --
            _ = Services.AddSingleton<IConverter>(sp => {
                return new SynchronizedConverter(new PdfTools());
            });
            // --
            _ = Services.AddSingleton<IApplicationService, CApplicationService>();
            _ = Services.AddSingleton<IBerkasService, CBerkasService>();
            _ = Services.AddSingleton<IChiperService, CChiperService>();
            _ = Services.AddSingleton<IConverterService, CConverterService>();
            _ = Services.AddSingleton<ICsvService, CCsvService>();
            _ = Services.AddSingleton<IFtpService, CFtpService>();
            _ = Services.AddSingleton<IGlobalService, CGlobalService>();
            _ = Services.AddSingleton<IHttpService, CHttpService>();
            _ = Services.AddSingleton<IKafkaService, CKafkaService>();
            _ = Services.AddSingleton<ILockerService, CLockerService>();
            _ = Services.AddSingleton<IPubSubService, CPubSubService>();
            _ = Services.AddSingleton<IQrBarService, CQrBarService>();
            _ = Services.AddSingleton<IRdlcService, CRdlcService>();
            _ = Services.AddSingleton<ISftpService, CSftpService>();
            _ = Services.AddSingleton<IStreamService, CStreamService>();
            _ = Services.AddSingleton<IZipService, CZipService>();

            // --
            // Setiap Request Cycle 1 Scope 1x New Object 1x Sesion Saja
            // --
            _ = Services.AddScoped<IApiDcListRepository, CApiDcListRepository>();
            _ = Services.AddScoped<IApiKeyRepository, CApiKeyRepository>();
            _ = Services.AddScoped<IApiTokenRepository, CApiTokenRepository>();
            _ = Services.AddScoped<IGeneralRepository, CGeneralRepository>();
            _ = Services.AddScoped<IMailRepository, CMailRepository>();
            _ = Services.AddScoped<IServerConfigRepository, CServerConfigRepository>();
            _ = Services.AddScoped<IUserRepository, CUserRepository>();
            // --
            _ = Services.AddScoped<IDefaultHandler, CDefaultHandler>();
            _ = Services.AddScoped<IEndpointProsesDataHandler, CEndpointProsesDataHandler>();
            _ = Services.AddScoped<IEndpointTarikDataHandler, CEndpointTarikDataHandler>();
        }

        public static void InitApp(WebApplication app, bool forceGcToCleanUpRamEveryRequest = false, int gcDelaySkipRunMinutes = 30) {
            App = app;
            GC_RUN_INTERVAL = gcDelaySkipRunMinutes;

            _ = Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER));

            if (forceGcToCleanUpRamEveryRequest) {
                _ = App.Use(async (context, next) => {
                    context.Response.OnCompleted(() => {
                        if (GC_RUN_LAST_DATE != null && (DateTime.Now - GC_RUN_LAST_DATE.Value).TotalMinutes < GC_RUN_INTERVAL) {
                            return Task.CompletedTask;
                        }

                        GC_RUN_LAST_DATE = DateTime.Now;

                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                        GC.WaitForPendingFinalizers();
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, false);

                        return Task.CompletedTask;
                    });

                    await next();
                });
            }
        }

        public static void UseHelmet() {
            _ = App.UseHelmet(o => {
                o.UseContentSecurityPolicy = false; // Buat Web Socket (Blazor SignalR, Socket.io, Web RTC)
                o.UseXContentTypeOptions = false; // Boleh Content-Sniff :: .mkv Dibaca .mp4
                o.UseReferrerPolicy = false; // Kalau Pakai Service Worker (Gak Set Origin, Tapi Referrer)
            });
        }

        public static void UseForwardedHeaders() {
            var options = new ForwardedHeadersOptions() {
                ForwardedHeaders = ForwardedHeaders.XForwardedHost
                    | ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
            };

            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
            options.ForwardLimit = null;

            _ = App.UseForwardedHeaders(options);
        }

        public static void UseNginxProxyPathSegment() {
            _ = App.Use((context, next) => {
                if (context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues prefix)) {
                    string proxyPath = prefix.Last();

                    context.Request.PathBase = new PathString(proxyPath.TrimEnd('/'));

                    if (context.Request.Path.StartsWithSegments(proxyPath, StringComparison.OrdinalIgnoreCase, out PathString remainingPath)) {
                        context.Request.Path = remainingPath;
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
        }

        public static void UseApiKeyMiddleware() {
            _ = App.UseMiddleware<ApiKeyMiddleware>();
        }

        public static void UseJwtMiddleware() {
            _ = App.UseMiddleware<JwtMiddleware>();
        }

        public static void Handle500ApiError<T>(string apiPrefix = "api") {
            if (string.IsNullOrWhiteSpace(apiPrefix)) {
                throw new Exception("API Prefix Wajib Di Isi");
            }

            API_PREFIX ??= apiPrefix;

            _ = App.Use(async (context, next) => {

                // Khusus API Path :: Akan Di Handle Error Dengan Balikan DataRowList JSON
                // Selain Itu Atau Jika Masih Ada Error Lain
                // Misal Di Catch Akan Terlempar Ke Halaman Error Bawaan UI

                if (!context.Request.Path.Value.StartsWith($"/{API_PREFIX}/", StringComparison.OrdinalIgnoreCase)) {
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

                        string xRequestTraceProxy = null;
                        if (response.Headers.ContainsKey("x-request-trace-proxy")) {
                            xRequestTraceProxy = response.Headers["x-request-trace-proxy"];
                        }
                        else {
                            if (request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                                string proxyPath = pathBase.Last();
                                if (!string.IsNullOrEmpty(proxyPath)) {
                                    xRequestTraceProxy = proxyPath;
                                    if (!response.HasStarted) {
                                        response.Headers.Append("x-request-trace-proxy", xRequestTraceProxy);
                                    }
                                }
                            }
                        }

                        string xRequestTraceActivity = null;
                        if (response.Headers.ContainsKey("x-request-trace-activity")) {
                            xRequestTraceActivity = response.Headers["x-request-trace-activity"];
                        }
                        else {
                            xRequestTraceActivity = Activity.Current?.Id;
                            if (!response.HasStarted) {
                                response.Headers.Append("x-request-trace-activity", xRequestTraceActivity);
                            }
                        }

                        string xRequestTraceId = null;
                        if (response.Headers.ContainsKey("x-request-trace-id")) {
                            xRequestTraceId = response.Headers["x-request-trace-id"];
                        }
                        else {
                            xRequestTraceId = context?.TraceIdentifier;
                            if (!response.HasStarted) {
                                response.Headers.Append("x-request-trace-id", xRequestTraceId);
                            }
                        }

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

                        if (context.Response.HasStarted) {
                            context.Abort();
                            return;
                        }

                        response.Clear();
                        response.StatusCode = StatusCodes.Status500InternalServerError;

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

                if (!context.Response.HasStarted) {
                    string apiPathRequested = context.Request.Path.Value;
                    if (!string.IsNullOrEmpty(apiPathRequested)) {
                        bool is404 = context.Response.StatusCode == StatusCodes.Status404NotFound;
                        bool isApi = apiPathRequested.StartsWith($"/{API_PREFIX}/", StringComparison.OrdinalIgnoreCase);

                        if (is404 && isApi) {
                            await context.Response.WriteAsJsonAsync(
                                new ResponseJsonSingle<ResponseJsonMessage>() {
                                    info = $"{StatusCodes.Status404NotFound} - Whoops :: API Tidak Ditemukan",
                                    result = new ResponseJsonMessage() {
                                        message = $"Dokumentasi Lengkap API Ada Di `/docs`"
                                    }
                                },
                                ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage
                            );
                        }
                    }
                }
            });

            RouteGroupBuilder routeGroup = App.MapGroup($"/{API_PREFIX}");

            if (redirectIndexToApi) {
                _ = App.Map("/", context => {
                    IApplicationService app = context.RequestServices.GetRequiredService<IApplicationService>();

                    string redirectUrl = $"/{API_PREFIX}";

                    if (!app.DebugMode && context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                        string proxyPath = pathBase.Last();
                        if (!string.IsNullOrEmpty(proxyPath)) {
                            redirectUrl = $"{proxyPath}{redirectUrl}";
                        }
                    }

                    context.Response.Redirect(redirectUrl, true, true);
                    return Task.CompletedTask;
                });
            }
            else {
                _ = App.Use(async (context, next) => {
                    await next();

                    if (!context.Response.HasStarted) {
                        string apiPathRequested = context.Request.Path.Value;
                        if (!string.IsNullOrEmpty(apiPathRequested)) {
                            bool is404 = context.Response.StatusCode == StatusCodes.Status404NotFound;
                            bool isStaticFile = Path.HasExtension(apiPathRequested);
                            bool isApi = apiPathRequested.StartsWith($"/{API_PREFIX}/", StringComparison.OrdinalIgnoreCase);

                            if (is404 && !isStaticFile && !isApi) {
                                context.Request.Path = "/index.html";
                                context.Response.StatusCode = 200;
                                await next();
                            }
                        }
                    }
                });
            }

            return routeGroup;
        }

        private static List<EJenisDc> CheckKafkaExcludeJenisDc(string excludeJenisDc) {
            List<EJenisDc> ls = null;

            if (!string.IsNullOrEmpty(excludeJenisDc)) {
                ls = [.. excludeJenisDc.Split(",").Where(d => !string.IsNullOrEmpty(d)).Select(d => {
                    string jenisDc = d.Trim().ToUpper();
                    EJenisDc _eJenisDc = EJenisDc.UNKNOWN;

                    if (Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                        _eJenisDc = eJenisDc;
                    }

                    return _eJenisDc;
                })];
            }

            return ls;
        }

        public static void AddKafkaProducerBackground(string hostPort, string topicName, short replication = 1, int partition = 1, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<EJenisDc> ls = CheckKafkaExcludeJenisDc(excludeJenisDc);
                return new KafkaProducer(sp, hostPort, topicName, replication, partition, suffixKodeDc, ls, pubSubName);
            });
        }

        public static void AddKafkaConsumerBackground(string hostPort, string topicName, string logTableName = null, string groupId = null, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<EJenisDc> ls = CheckKafkaExcludeJenisDc(excludeJenisDc);
                return new KafkaConsumer(sp, hostPort, topicName, logTableName, groupId, suffixKodeDc, ls, pubSubName);
            });
        }

        public static void AddKafkaAutoProducerConsumerBackground(IDictionary<string, KafkaInstance> kafkaSettings) {
            if (kafkaSettings != null) {
                foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                    if (ks.Key.StartsWith("PRODUCER_")) {
                        AddKafkaProducerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.REPLICATION, ks.Value.PARTITION, ks.Value.SUFFIX_KODE_DC, ks.Value.EXCLUDE_JENIS_DC, ks.Key);
                    }
                }

                foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                    if (ks.Key.StartsWith("CONSUMER_")) {
                        AddKafkaConsumerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.LOG_TABLE_NAME, ks.Value.GROUP_ID, ks.Value.SUFFIX_KODE_DC, ks.Value.EXCLUDE_JENIS_DC, ks.Key);
                    }
                }
            }
        }

        public static ScheduleBuilder ScheduleJob(string cronExpression) {
            return Schedules.GetOrAdd(cronExpression, _ => new ScheduleBuilder(cronExpression, Services));
        }

        public static void StartJobScheduler() {
            // Default Job Bawaan ~
            _ = ScheduleJob("* * * * *").AddJob<CleanUpJobScheduler>();

            _ = Services.AddSingleton<IJobTracker, CJobTracker>();

            IEnumerable<CronJob> jobs = [.. Schedules.Values.SelectMany(s => s._jobs)];

            _ = Services.AddSingleton(jobs);
            _ = Services.AddSingleton<CronScheduler>();

            _ = Services.AddHostedService(sp => sp.GetRequiredService<CronScheduler>());
        }

    }

}