using bifeldy_lib_90.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Reporting.NETCore;
using System.Data;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WkHtmlToPdfDotNet;

namespace bifeldy_lib_90.Services {

    public interface IRdlcService {
        IDictionary<string, RdlcInfo> FileType { get; }
        LocalReport CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null);
        ReportDataSource CreateReportDataSource(string name, DataTable dt);
        ReportDataSource CreateReportDataSource<T>(string name, IEnumerable<T> dt);
        HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel);
        ReportParameter[] CreateReportParameter(IDictionary<string, string> dict);
        RdlcReport GeneratePdfWordExcelHtmlReport(string rdlcName, DataTable dt, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5", MarginSettings margin = null, Orientation pageOrientation = Orientation.Portrait, PaperKind paperType = PaperKind.Custom);
        RdlcReport GeneratePdfWordExcelHtmlReport<T>(string rdlcName, IEnumerable<T> ls, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5", MarginSettings margin = null, Orientation pageOrientation = Orientation.Portrait, PaperKind paperType = PaperKind.Custom);
        Task<Stream> GeneratePdfWordExcelHtmlReportExternalRdlcProcessStreamed<T>(CancellationToken ct, JsonTypeInfo<RdlcRequestWrapper<T>> typeInfo, RdlcRequestWrapper<T> rdlcDataWithParam, string rdlcPath, string datasetName, string fileType = "PDF", string externalRdlcProcessPath = null);
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

        public LocalReport CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            string rdlcPath = Path.Combine(this._app.AppLocation, "Rdlcs", rdlcName);

            if (!File.Exists(rdlcPath)) {
                throw new FileNotFoundException($"File RDLC {rdlcName} Tidak Ditemukan!", rdlcPath);
            }

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

            return report;
        }

        public ReportDataSource CreateReportDataSource(string name, DataTable dt) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            return new(name, dt);
        }

        public ReportDataSource CreateReportDataSource<T>(string name, IEnumerable<T> ls) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            return new(name, ls);
        }

        public HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            return new HtmlToPdfDocument() {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = reportModel.PageOrientation,
                    Margins = reportModel.Margins,
                    DocumentTitle = reportModel.DisplayName,
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = reportModel.HtmlContent,
                        WebSettings = {
                            DefaultEncoding = "utf-8"
                        }
                    }
                }
            };
        }

        public ReportParameter[] CreateReportParameter(IDictionary<string, string> dict) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            var ls = new List<ReportParameter>();
            foreach (KeyValuePair<string, string> kvp in dict) {
                ls.Add(new ReportParameter(kvp.Key, kvp.Value));
            }

            return [.. ls];
        }

        private MarginSettings SetupPage() {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            return new MarginSettings() {
                Top = 1,
                Bottom = 1,
                Left = 1,
                Right = 1,
                Unit = Unit.Centimeters
            };
        }

        private RdlcReport GenerateReport(
            string rdlcName,
            ReportDataSource rds,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            LocalReport report = this.CreateLocalReport(rdlcName, rds, param);

            var model = new RdlcReport() {
                DisplayName = report.DisplayName,
                Margins = margin,
                PageOrientation = pageOrientation,
                PaperType = paperType,
                RenderType = this.FileType[fileType].saveType
            };

            if (fileType == "PDF" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                model.RenderType = "HTML5";
                model.HtmlContent = Encoding.UTF8.GetString(report.Render(model.RenderType));
                model.Report = this._converter.HtmlToPdf(this.GenerateHtmlReport(model));
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
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            margin ??= this.SetupPage();
            ReportDataSource rds = this.CreateReportDataSource(dsName, dt);
            return this.GenerateReport(rdlcName, rds, param, fileType, margin, pageOrientation, paperType);
        }

        public RdlcReport GeneratePdfWordExcelHtmlReport<T>(
            string rdlcName,
            IEnumerable<T> ls,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            if (!RuntimeFeature.IsDynamicCodeSupported) {
                throw new Exception("Hanya bisa dijalankan menggunakan JIT, bukan AOT");
            }

            margin ??= this.SetupPage();
            ReportDataSource rds = this.CreateReportDataSource(dsName, ls);
            return this.GenerateReport(rdlcName, rds, param, fileType, margin, pageOrientation, paperType);
        }

        public async Task<Stream> GeneratePdfWordExcelHtmlReportExternalRdlcProcessStreamed<T>(
            CancellationToken ct,
            JsonTypeInfo<RdlcRequestWrapper<T>> typeInfo,
            RdlcRequestWrapper<T> rdlcDataWithParam,
            string rdlcPath,
            string datasetName,
            string fileType = "PDF",
            string externalRdlcProcessPath = null
        ) {
            if (string.IsNullOrEmpty(externalRdlcProcessPath)) {
                externalRdlcProcessPath = Path.Combine(this._app.AppLocation, "sidecar", "rdlcs_generator");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !externalRdlcProcessPath.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)) {
                externalRdlcProcessPath += ".exe";
            }

            var psi = new ProcessStartInfo() {
                FileName = externalRdlcProcessPath,
                Arguments = $"\"{rdlcPath}\" \"{datasetName}\" \"{fileType}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(externalRdlcProcessPath)
            };

            var process = new Process() {
                StartInfo = psi
            };

            _ = process.Start();

            // Jika dibatalkan, bunuh proses segera
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
                    Stream stdin = process.StandardInput.BaseStream;

                    await JsonSerializer.SerializeAsync(stdin, rdlcDataWithParam, typeInfo, ct);
                    await stdin.FlushAsync(ct);

                    process.StandardInput.Close();
                }
                catch (Exception ex) {
                    this._logger.LogError("[RDLC_SEND_ERR] {ex}", ex.Message);
                }
            }, ct);

            if (process.HasExited) {
                throw new Exception("Rdlcs generator gagal dijalankan sebelum memproses data.");
            }

            var ms = new MemoryStream();

            try {
                await process.StandardOutput.BaseStream.CopyToAsync(ms, ct);

                await sendSerializedData;
                await process.WaitForExitAsync(ct);

                if (process.ExitCode != 0) {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Sidecar Exit Code {process.ExitCode}: {error}");
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[RDLC_PROCESS_ERR] {ex}", ex.Message);
                ms.Dispose();
                throw;
            }
            finally {
                if (!process.HasExited) {
                    process.Kill(true);
                }
            }

            ms.Position = 0;

            return ms;
        }

    }

}