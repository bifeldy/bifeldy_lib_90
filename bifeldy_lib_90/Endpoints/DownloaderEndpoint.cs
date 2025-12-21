using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.JobSchedulers;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;

namespace bifeldy_lib_90.Endpoints {

    public static class DownloaderEndpoint {

        private static readonly string ROUTE_GROUP = "/downloader";

        [UnconditionalSuppressMessage(
            "Trimming", "IL2026",
            Justification = "Minimal API handler is static and AOT-safe"
        )]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050",
            Justification = "Minimal API handler uses static delegate with known types"
        )]
        public static RouteGroupBuilder MapDownloaderEndpoints(this RouteGroupBuilder routeGroupBuilder) {
            string documentName = "latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty);

            RouteGroupBuilder apiGroup = routeGroupBuilder
                .MapGroupTagDescription(
                    ROUTE_GROUP, "__",
                    "Fitur standar bawaan untuk unduh berkas ~"
                )
                .WithGroupNames(documentName);

            _ = apiGroup.MapGet("/", Downloader)
                .WithSummary("Downloader")
                .WithDescription("Untuk check `hash md5` file kemudian unduh")
                .Produces<ResponseJsonSingle<Dictionary<string, object>>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status206PartialContent)
                .Produces<ResponseJsonSingle<ResponseJsonMessage>>(StatusCodes.Status403Forbidden)
                .Produces<ResponseJsonSingle<ResponseJsonMessage>>(StatusCodes.Status404NotFound)
                .Produces<ResponseJsonSingle<ResponseJsonMessage>>(StatusCodes.Status410Gone)
                .Produces(StatusCodes.Status416RangeNotSatisfiable);

            return apiGroup;
        }

        private static async Task<IResult> Downloader(
            HttpContext http,
            [FromServices] IDistributedCache cache,
            [FromServices] IApplicationService @as,
            [FromServices] IGlobalService gs,
            [FromServices] IChiperService chiper,
            [FromServices] IConverterService converter,
            [FromServices] ILockerService locker,
            [FromServices] IRdlcService rdlc,
            [FromServices] CronScheduler scheduler,
            [FromQuery] string fileName = "[FANSUB] Blue AV (BD 720p AAC).mkv",
            [FromQuery] string fileType = "video/x-matroska",
            [FromQuery] string completedOnly = "true",
            [FromQuery] string compareMd5 = "08e6e1d1"
        ) {
            string cacheKey = http.Request.Path;

            bool isCompletedOnly = bool.TryParse(completedOnly.ToString(), out bool _completedOnly) && _completedOnly;

            try {
                var user = (JwtSession)http.Items["user"];

                if (string.IsNullOrEmpty(fileName)) {
                    if (user.role > ESessionRole.USER_SD_SSD_3) {
                        return Results.Json(
                            new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = $"{StatusCodes.Status403Forbidden} - Hash Files",
                                result = new ResponseJsonMessage() {
                                    message = "Harap input nama file ?fileName=blablabla.ext"
                                }
                            },
                            ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                            MediaTypeNames.Application.Json,
                            StatusCodes.Status403Forbidden
                        );
                    }

                    Dictionary<string, object> fileHash = null;

                    try {
                        _ = await locker.SemaphoreGlobalApp("DOWNLOADER").WaitAsync(-1);

                        string result = await cache.GetStringAsync(cacheKey);
                        if (string.IsNullOrEmpty(result?.Trim())) {
                            fileHash = [];

                            IEnumerable<FileInfo> fileInfos = Directory.GetFiles(@as.AppLocation, "*", SearchOption.AllDirectories)
                                .Where(p => {
                                    string dataPath = Path.Combine(@as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER);
                                    return !p.Contains("appsettings.json") && !p.Contains(dataPath);
                                })
                                .Select(p => new FileInfo(p))
                                .OrderBy(fi => fi.Name);
                                // .OrderByDescending(fi => fi.LastWriteTime);

                            foreach (FileInfo fi in fileInfos) {
                                string crc32 = chiper.CalculateCRC32File(fi.FullName);
                                if (crc32 != null) {
                                    string key = fi.FullName.Replace(@as.AppLocation, string.Empty);
                                    fileHash[key] = crc32;
                                }
                            }

                            result = converter.ObjectToJson(fileHash);
                            result = result?.Trim();

                            if (!string.IsNullOrEmpty(result)) {
                                await cache.SetStringAsync(cacheKey, result, new DistributedCacheEntryOptions() {
                                    SlidingExpiration = TimeSpan.FromMinutes(10),
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                                });
                            }
                        }
                        else {
                            fileHash = (Dictionary<string, object>)converter.JsonToObject(result);
                        }
                    }
                    finally {
                        _ = locker.SemaphoreGlobalApp("DOWNLOADER").Release();
                    }

                    if (fileHash.Count <= 0) {
                        return Results.NotFound(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"{StatusCodes.Status404NotFound} - Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "Tidak Tersedia Pembaharuan"
                            }
                        });
                    }

                    return Results.Ok(new ResponseJsonSingle<Dictionary<string, object>>() {
                        info = $"{StatusCodes.Status200OK} - Hash Files",
                        result = fileHash
                    });
                }
                else {
                    while (fileName.StartsWith(".") || fileName.StartsWith("/") || fileName.StartsWith("\\") || fileName.StartsWith("~")) {
                        fileName = fileName[1..];
                    }

                    string dirPath = @as.AppLocation;
                    string mimeType = null;

                    fileType = fileType?.ToUpper();
                    switch (fileType) {
                        case "CSV":
                            dirPath = gs.CsvFolderPath;
                            mimeType = "text/csv";
                            break;
                        case "ZIP":
                            dirPath = gs.ZipFolderPath;
                            mimeType = "application/x-zip";
                            break;
                        default:
                            bool isFound = false;

                            if (!string.IsNullOrEmpty(fileType)) {
                                if (rdlc.FileType.ContainsKey(fileType)) {
                                    isFound = true;
                                    dirPath = gs.TempFolderPath;
                                    mimeType = rdlc.FileType[fileType].contentType;
                                }
                            }

                            if (!isFound) {
                                if (user.role > ESessionRole.USER_SD_SSD_3) {
                                    return Results.Json(
                                        new ResponseJsonSingle<ResponseJsonMessage>() {
                                            info = $"{StatusCodes.Status403Forbidden} - Hash Files",
                                            result = new ResponseJsonMessage() {
                                                message = "Harap input tipe file ?fileType=csv / ?fileType=zip"
                                            }
                                        },
                                        ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                                        MediaTypeNames.Application.Json,
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                dirPath = @as.AppLocation;
                                mimeType = null;
                            }

                            break;
                    }

                    string filePath = Path.Combine(dirPath, fileName);

                    if (!File.Exists(filePath)) {
                        return Results.NotFound(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"{StatusCodes.Status404NotFound} - Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "File Tidak Ditemukan"
                            }
                        });
                    }

                    var fi = new FileInfo(filePath);
                    bool isFileReady = false;

                    if (isCompletedOnly) {
                        string jobName = $"ExportFile___{fi.Name}";

                        CompletedJob jobCompleted = scheduler.CheckJobIsCompleted(jobName);
                        if (jobCompleted != null) {
                            isFileReady = jobCompleted.Success;
                        }

                        if (!isFileReady) {
                            isFileReady = File.Exists(fi.FullName);
                        }
                    }
                    else {
                        if (File.Exists(fi.FullName)) {
                            isFileReady = true;
                        }
                    }

                    if (!isFileReady) {
                        return Results.Json(
                            new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = $"{StatusCodes.Status410Gone} - Hash Files",
                                result = new ResponseJsonMessage() {
                                    message = "File Belum Tersedia"
                                }
                            },
                            ResponseJsonSerializerContext.Default.ResponseJsonSingleResponseJsonMessage,
                            MediaTypeNames.Application.Json,
                            StatusCodes.Status410Gone
                        );
                    }

                    string checksum = chiper.CalculateMD5File(fi.FullName);
                    http.Response.Headers.Append("md5", checksum);

                    if (string.IsNullOrEmpty(mimeType)) {
                        string tempPath = Path.GetTempPath();
                        string tempFileName = Path.GetTempFileName();

                        string destinationFilePath = Path.Combine(tempPath, Path.GetFileName(tempFileName));
                        File.Copy(fi.FullName, destinationFilePath, true);

                        mimeType = chiper.GetMimeFile(destinationFilePath);

                        File.Delete(destinationFilePath);
                    }

                    if (!string.IsNullOrEmpty(compareMd5)) {
                        if (compareMd5 == checksum) {
                            return Results.NoContent();
                        }
                    }

                    using (FileStream fs = File.OpenRead(fi.FullName)) {
                        return Results.Stream(fs, mimeType, fi.Name, fi.LastWriteTime, enableRangeProcessing: true);
                    }
                }
            }
            catch {
                cache.Remove(cacheKey);

                return Results.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{StatusCodes.Status400BadRequest} - Hash File",
                    result = new ResponseJsonMessage() {
                        message = "Terjadi kesalahan saat proses data!"
                    }
                });
            }
        }

    }

}
