using bifeldy_lib_90.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Reporting.NETCore;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WkHtmlToPdfDotNet;

namespace bifeldy_lib_90.Services {

    public interface IRdlcService {
        IDictionary<string, RdlcInfo> FileType { get; }
        (LocalReport, string, string, string, string, string, string) CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null);
        ReportDataSource CreateReportDataSource(string name, DataTable dt);
        ReportDataSource CreateReportDataSource<T>(string name, IEnumerable<T> dt);
        HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel, string width, string height, double top, double bottom, double left, double right);
        ReportParameter[] CreateReportParameter(IDictionary<string, string> dict);
        RdlcInfoWrapper CreateInfoWrapper(IDictionary<string, string> dict);
        RdlcReport GeneratePdfWordExcelHtmlReport(string rdlcName, DataTable dt, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5");
        RdlcReport GeneratePdfWordExcelHtmlReport<T>(string rdlcName, IEnumerable<T> ls, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5");
        Task GeneratePdfWordExcelHtmlReportExternal<T>(CancellationToken ct, Stream streamDestination, IAsyncEnumerable<T> dataStream, JsonTypeInfo<T> typeInfo, RdlcInfoWrapper rdlcDataWithParam, string rdlcPath, string datasetName, string fileType = "PDF", string rdlcGeneratorExecutablePath = null);
    }

    public sealed class CRdlcService : IRdlcService {

        private readonly ILogger<CRdlcService> _logger;

        private readonly IApplicationService _app;
        private readonly IConverterService _converter;

        public IDictionary<string, RdlcInfo> FileType { get; } = new Dictionary<string, RdlcInfo>(StringComparer.InvariantCultureIgnoreCase) {
            {
                "PDF", new() {
                    contentType = MediaTypeNames.Application.Pdf,
                    saveType = "PDF"
                }
            },
            {
                "DOCX", new() {
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    saveType = "WORDOPENXML"
                }
            },
            {
                "XLSX", new() {
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    saveType = "EXCELOPENXML"
                }
            },
            {
                "HTML", new() {
                    contentType = "text/html",
                    saveType = "HTML5"
                }
            }
        };

        public CRdlcService(
            ILogger<CRdlcService> logger,
            IApplicationService app,
            IConverterService converter
        ) {
            this._logger = logger;
            this._app = app;
            this._converter = converter;
        }

        public (LocalReport, string, string, string, string, string, string) CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            string rdlcPath = Path.Combine(this._app.AppLocation, "Rdlcs", rdlcName);

            if (!File.Exists(rdlcPath)) {
                throw new FileNotFoundException($"File RDLC {rdlcName} Tidak Ditemukan!", rdlcPath);
            }

            string width = null;
            string height = null;
            string topMargin = null;
            string bottomMargin = null;
            string leftMargin = null;
            string rightMargin = null;

            byte[] rdlcBytes = File.ReadAllBytes(rdlcPath);
            using (var ms = new MemoryStream(rdlcBytes)) {
                var xdoc = XDocument.Load(ms);
                XNamespace ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

                XElement pageElement = xdoc.Descendants(ns + "Page").FirstOrDefault();
                if (pageElement != null) {
                    // Ambil Dimensi
                    width = pageElement.Element(ns + "PageWidth")?.Value;
                    height = pageElement.Element(ns + "PageHeight")?.Value;

                    // Ambil Margin (Mereka bertetangga dengan PageWidth)
                    topMargin = pageElement.Element(ns + "TopMargin")?.Value;
                    bottomMargin = pageElement.Element(ns + "BottomMargin")?.Value;
                    leftMargin = pageElement.Element(ns + "LeftMargin")?.Value;
                    rightMargin = pageElement.Element(ns + "RightMargin")?.Value;
                }
            }

            if (width == null || height == null) {
                throw new Exception($"Ukuran width ({width}) / height ({height}) Masih NULL");
            }

            width = width.Trim().ToLower().Replace(",", ".");
            height = height.Trim().ToLower().Replace(",", ".");

            var report = new LocalReport() {
                ReportPath = rdlcPath
            };

            if (ds != null) {
                report.DisplayName = ds.Name;
                report.DataSources.Add(ds);
            }

            if (param != null) {
                report.SetParameters(param);
            }

            return (report, width, height, topMargin, bottomMargin, leftMargin, rightMargin);
        }

        public ReportDataSource CreateReportDataSource(string name, DataTable dt) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            return new(name, dt);
        }

        public ReportDataSource CreateReportDataSource<T>(string name, IEnumerable<T> ls) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            return new(name, ls);
        }

        public HtmlToPdfDocument GenerateHtmlReport(RdlcReport model, string width, string height, double top, double bottom, double left, double right) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var htmlToPdfDocument = new HtmlToPdfDocument() {
                GlobalSettings = new GlobalSettings() {
                    DocumentTitle = model.DisplayName,
                    ColorMode = ColorMode.Color,
                    Margins = new MarginSettings() {
                        Top = top,
                        Bottom = bottom,
                        Left = left,
                        Right = right,
                        Unit = Unit.Inches
                    },
                    PaperSize = new PechkinPaperSize(width, height),
                    ImageDPI = 300
                }
            };

            htmlToPdfDocument.Objects.Add(new ObjectSettings() {
                HtmlContent = model.HtmlContent,
                WebSettings = new WebSettings() {
                    DefaultEncoding = "utf-8",
                    EnableIntelligentShrinking = false
                }
            });

            return htmlToPdfDocument;
        }

        public ReportParameter[] CreateReportParameter(IDictionary<string, string> dict) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            var ls = new List<ReportParameter>();
            foreach (KeyValuePair<string, string> kvp in dict) {
                ls.Add(new ReportParameter(kvp.Key, kvp.Value));
            }

            return [.. ls];
        }

        public RdlcInfoWrapper CreateInfoWrapper(IDictionary<string, string> dict) {
            return new RdlcInfoWrapper(dict);
        }

        private double? ParseDimensionToInch(string dim) {
            if (string.IsNullOrEmpty(dim)) {
                return null;
            }

            // Bersihkan koma jadi titik, lowercase, trim
            dim = dim.Trim().ToLower().Replace(",", ".");

            Match match = Regex.Match(dim, @"([\d\.]+)\s*(cm|in|mm|pt|pc)");
            if (match.Success) {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)) {
                    string unit = match.Groups[2].Value;
                    return unit switch {
                        "cm" => val / 2.54,
                        "mm" => val / 25.4,
                        "in" => val,
                        "pt" => val / 72.0,
                        "pc" => val / 6.0,
                        _ => val / 2.54 // Default cm kalau unit aneh
                    };
                }
            }

            return null;
        }

        private RdlcReport GenerateReport(
            string rdlcName,
            ReportDataSource rds,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML"
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            (
                LocalReport report,
                string width,
                string height,
                string topMargin,
                string bottomMargin,
                string leftMargin,
                string rightMargin
            ) = this.CreateLocalReport(rdlcName, rds, param);

            var model = new RdlcReport() {
                DisplayName = report.DisplayName,
                RenderType = this.FileType[fileType].saveType
            };

            if (fileType == "PDF" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                model.RenderType = "HTML5";
                string rawHtml = Encoding.UTF8.GetString(report.Render(model.RenderType));

                double? wIn = this.ParseDimensionToInch(width);
                double? hIn = this.ParseDimensionToInch(height);

                if (wIn == null || hIn == null) {
                    throw new Exception($"Ukuran wIn ({wIn}) / hIn ({hIn}) Masih NULL");
                }

                double mTop = this.ParseDimensionToInch(topMargin) ?? 1.0;
                double mBottom = this.ParseDimensionToInch(bottomMargin) ?? 1.0;
                double mLeft = this.ParseDimensionToInch(leftMargin) ?? 1.0;
                double mRight = this.ParseDimensionToInch(rightMargin) ?? 1.0;

                string wStr = wIn?.ToString("0.##", CultureInfo.InvariantCulture) + "in";
                string hStr = hIn?.ToString("0.##", CultureInfo.InvariantCulture) + "in";

                string dynamicCss = $@"
                    <style>
                        @page {{
                            size: {wStr} {hStr};
                            margin: 0;
                        }}
                        body {{ 
                            margin: 0 !important; 
                            padding: 0 !important; 
                            width: auto !important;
                            overflow: hidden !important;
                        }}
                        table {{ 
                            width: 100% !important; 
                            table-layout: fixed !important; 
                            border-collapse: collapse !important; 
                        }}
                    </style>
                ";

                int headCloseIndex = rawHtml.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
                if (headCloseIndex >= 0) {
                    model.HtmlContent = rawHtml.Insert(headCloseIndex, dynamicCss);
                }
                else {
                    model.HtmlContent = dynamicCss + rawHtml;
                }

                HtmlToPdfDocument htmlToPdfDocument = this.GenerateHtmlReport(model, wStr, hStr, mTop, mBottom, mLeft, mRight);

                model.Report = this._converter.HtmlToPdf(htmlToPdfDocument);
            }
            else {
                model.Report = report.Render(model.RenderType);
            }

            return model;
        }

        public RdlcReport GeneratePdfWordExcelHtmlReport(
            string rdlcName,
            DataTable dt,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML"
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            ReportDataSource rds = this.CreateReportDataSource(dsName, dt);
            return this.GenerateReport(rdlcName, rds, param, fileType);
        }

        public RdlcReport GeneratePdfWordExcelHtmlReport<T>(
            string rdlcName,
            IEnumerable<T> ls,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML"
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
            }

            ReportDataSource rds = this.CreateReportDataSource(dsName, ls);
            return this.GenerateReport(rdlcName, rds, param, fileType);
        }

        public async Task GeneratePdfWordExcelHtmlReportExternal<T>(
            CancellationToken ct,
            Stream streamDestination,
            IAsyncEnumerable<T> dataStream,
            JsonTypeInfo<T> typeInfo,
            RdlcInfoWrapper rdlcDataWithParam,
            string rdlcPath,
            string datasetName,
            string fileType = "PDF",
            string rdlcGeneratorExecutablePath = null
        ) {
            try {
                if (string.IsNullOrEmpty(rdlcDataWithParam.DataFilePath)) {
                    string dataFilePath = $"rdlc_data_{Guid.NewGuid()}.tmp";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists("/dev/shm")) {
                        dataFilePath = Path.Combine("/dev/shm", dataFilePath);
                    }
                    else {
                        dataFilePath = Path.Combine(Path.GetTempPath(), dataFilePath);
                    }

                    rdlcDataWithParam.DataFilePath = dataFilePath;
                }

                using (var fs = new FileStream(rdlcDataWithParam.DataFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096)) {
                    using (var writer = new Utf8JsonWriter(fs)) {
                        writer.WriteStartArray();

                        await foreach (T row in dataStream) {
                            JsonSerializer.Serialize(writer, row, typeInfo);
                        }

                        writer.WriteEndArray();
                        await writer.FlushAsync();
                    }
                }

                rdlcGeneratorExecutablePath ??= Path.Combine(this._app.AppLocation, "sidecar", "rdlcs_generator");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !rdlcGeneratorExecutablePath.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)) {
                    rdlcGeneratorExecutablePath += ".exe";
                }

                var psi = new ProcessStartInfo() {
                    FileName = rdlcGeneratorExecutablePath,
                    Arguments = $"\"{rdlcPath}\" \"{datasetName}\" \"{fileType}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(rdlcGeneratorExecutablePath)
                };

                using (var process = Process.Start(psi)) {
                    _ = ct.Register(() => {
                        try {
                            if (!process.HasExited) {
                                process.Kill(true);
                            }
                        }
                        catch (Exception ex) {
                            this._logger.LogError("[RDLC_TOKEN_ERR] {ex}", ex.Message);
                        }
                    });

                    var sendSerializedData = Task.Run(async () => {
                        try {
                            using (StreamWriter writter = process.StandardInput) {
                                await JsonSerializer.SerializeAsync(
                                    writter.BaseStream,
                                    rdlcDataWithParam,
                                    RdlcInfoWrapperJsonSerializerContext.Default.RdlcInfoWrapper,
                                    ct
                                );
                                await writter.FlushAsync(ct);
                            }
                        }
                        catch (Exception ex) {
                            this._logger.LogError("[RDLC_SEND_ERR] {ex}", ex.Message);
                        }
                    }, ct);

                    if (process.HasExited) {
                        throw new Exception("Rdlcs Generator Gagal Dijalankan Sebelum Memproses Data.");
                    }

                    try {
                        Task receiveBinaryData = process.StandardOutput.BaseStream.CopyToAsync(streamDestination, ct);

                        await Task.WhenAll(sendSerializedData, receiveBinaryData);
                        await process.WaitForExitAsync(ct);

                        if (process.ExitCode != 0) {
                            string error = await process.StandardError.ReadToEndAsync();
                            throw new Exception($"Sidecar Exit Code {process.ExitCode}: {error}");
                        }
                    }
                    finally {
                        if (!process.HasExited) {
                            process.Kill(true);
                        }
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[RDLC_PROCESS_ERR] {ex}", ex.Message);
                throw;
            }
            finally {
                if (File.Exists(rdlcDataWithParam.DataFilePath)) {
                    File.Delete(rdlcDataWithParam.DataFilePath);
                }
            }
        }

    }

}