using bifeldy_lib_90.Models;
using Microsoft.Reporting.NETCore;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using WkHtmlToPdfDotNet;

// JANGAN gunakan kode asinkron (await) SEBELUM setting AssemblyResolve dipasang
// Kita pakai Main sinkron dulu atau bungkus logic di dalam fungsi lain

public static class Program {

    public static void Main(string[] args) {
        // Fix: Cari DLL di folder executable itu sendiri secara eksplisit
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
            string folderPath = AppContext.BaseDirectory;
            string assemblyName = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string assemblyPath = Path.Combine(folderPath, assemblyName);

            if (File.Exists(assemblyPath)) {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        };

        // Panggil logic utama di fungsi terpisah agar JIT tidak memuat System.Runtime terlalu dini
        RunAsync(args).GetAwaiter().GetResult();
    }

    private static async Task RunAsync(string[] args) {
        AppDomain.CurrentDomain.AssemblyResolve += (_sender, _args) => {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            string assemblyPath = Path.Combine(folderPath, new AssemblyName(_args.Name).Name + ".dll");
            if (File.Exists(assemblyPath)) {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        };

        // Ambil Args: [0]rdlcPath, [1]datasetName, [2]fileType
        if (args.Length < 3) {
            Environment.Exit(1);
        }

        string rdlcPath = args[0];
        string datasetName = args[1];
        string fileType = args[2].ToUpper();

        try {
            using (Stream stdin = Console.OpenStandardInput()) {
                JsonNode? root = await JsonNode.ParseAsync(stdin);
                if (root == null) {
                    throw new Exception("Data JSON kosong");
                }

                JsonArray? dataRows = root?["DataRowList"]?.AsArray();
                JsonObject? parameters = root?["Parameters"]?.AsObject();

                var dt = new DataTable(datasetName);
                if (dataRows?.Count > 0) {
                    foreach (KeyValuePair<string, JsonNode?> prop in dataRows[0]!.AsObject()) {
                        _ = dt.Columns.Add(prop.Key, typeof(object));
                    }

                    foreach (JsonNode? row in dataRows) {
                        DataRow dr = dt.NewRow();

                        foreach (KeyValuePair<string, JsonNode?> prop in row!.AsObject()) {
                            JsonValue? val = prop.Value?.AsValue();

                            if (val != null) {
                                if (val.TryGetValue(out string? s)) {
                                    dr[prop.Key] = s;
                                }
                                else if (val.TryGetValue(out bool b)) {
                                    dr[prop.Key] = b;
                                }
                                else if (val.TryGetValue(out int i)) {
                                    dr[prop.Key] = i;
                                }
                                else if (val.TryGetValue(out long l)) {
                                    dr[prop.Key] = l;
                                }
                                else if (val.TryGetValue(out float f)) {
                                    dr[prop.Key] = f;
                                }
                                else if (val.TryGetValue(out double d)) {
                                    dr[prop.Key] = d;
                                }
                                else if (val.TryGetValue(out decimal m)) {
                                    dr[prop.Key] = m;
                                }
                                else if (val.TryGetValue(out DateTime dtm)) {
                                    dr[prop.Key] = dtm;
                                }
                                else if (val.TryGetValue(out DateOnly dto)) {
                                    dr[prop.Key] = dto;
                                }
                                else {
                                    // Dictionary, Array, Object
                                    throw new NotSupportedException("Unsupported JSON primitive");
                                }
                            }
                            else {
                                dr[prop.Key] = DBNull.Value;
                            }
                        }

                        dt.Rows.Add(dr);
                    }
                }

                using (var report = new LocalReport()) {
                    byte[] rdlcBytes = await File.ReadAllBytesAsync(rdlcPath);
                    using (var rdlcStream = new MemoryStream(rdlcBytes)) {
                        report.LoadReportDefinition(rdlcStream);

                        var rds = new ReportDataSource(datasetName, dt);

                        report.DisplayName = rds.Name;
                        report.DataSources.Add(rds);

                        if (parameters != null) {
                            var reportParams = new List<ReportParameter>();

                            foreach (KeyValuePair<string, JsonNode?> p in parameters) {
                                reportParams.Add(new ReportParameter(p.Key, p.Value?.ToString()));
                            }

                            report.SetParameters(reportParams);
                        }

                        string? format = null;
                        switch (fileType) {
                            case "PDF":
                                format = "PDF";
                                break;
                            case "DOCX":
                                format = "WORDOPENXML";
                                break;
                            case "XLSX":
                                format = "EXCELOPENXML";
                                break;
                            case "HTML":
                                format = "HTML5";
                                break;
                            default:
                                throw new Exception("Format tidak tersedia");
                        }

                        var model = new RdlcReport() {
                            DisplayName = report.DisplayName,
                            Margins = new MarginSettings() {
                                Top = 1,
                                Bottom = 1,
                                Left = 1,
                                Right = 1,
                                Unit = Unit.Centimeters
                            },
                            PageOrientation = Orientation.Portrait,
                            PaperType = PaperKind.Custom,
                            RenderType = format
                        };

                        if (fileType == "PDF" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                            model.RenderType = "HTML5";
                            model.HtmlContent = Encoding.UTF8.GetString(report.Render(model.RenderType));

                            var htmlToPdfDocument = new HtmlToPdfDocument() {
                                GlobalSettings = {
                            ColorMode = ColorMode.Color,
                            Orientation = model.PageOrientation,
                            Margins = model.Margins,
                            DocumentTitle = model.DisplayName,
                        },
                                Objects = {
                            new ObjectSettings() {
                                HtmlContent = model.HtmlContent,
                                WebSettings = {
                                    DefaultEncoding = "utf-8"
                                }
                            }
                        }
                            };

                            using (var converter = new SynchronizedConverter(new PdfTools())) {
                                model.Report = converter.Convert(htmlToPdfDocument);
                            }
                        }
                        else {
                            model.Report = report.Render(model.RenderType);
                        }

                        byte[] reportData = model.Report;

                        using (Stream stdout = Console.OpenStandardOutput()) {
                            await stdout.WriteAsync(reportData, 0, reportData.Length);
                            await stdout.FlushAsync();
                        }

                        await Task.Delay(100);
                        Environment.Exit(0);
                    }
                }
            }
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());
            Environment.Exit(1);
        }
    }

}
