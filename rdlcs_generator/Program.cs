using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Models;
using Microsoft.Reporting.NETCore;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

    private static Type GetTypeFromJsonKind(JsonElement el) {
        switch (el.ValueKind) {
            case JsonValueKind.Number:
                return typeof(decimal);

            case JsonValueKind.String:
                if (el.TryGetDateTime(out _)) {
                    return typeof(DateTime);
                }

                return typeof(string);

            case JsonValueKind.True:
            case JsonValueKind.False:
                return typeof(bool);

            // case JsonValueKind.Null:
            //     return typeof(DBNull);

            default:
                return typeof(object);
        }
    }

    private static object GetValueFromJsonElement(JsonElement el) {
        switch (el.ValueKind) {
            case JsonValueKind.Number:
                return el.GetDecimal().RemoveTrail();

            case JsonValueKind.String:
                if (el.TryGetDateTime(out DateTime dt)) {
                    return dt;
                }

                return el.GetString()!;

            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return DBNull.Value;

            default:
                return el.ToString();
        }
    }

    private static double? ParseDimensionToInch(string? dim) {
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

    // Ambil Args: [0]rdlcPath, [1]datasetName, [2]fileType
    private static async Task RunAsync(string[] args) {
        if (args.Length < 3) {
            Environment.Exit(-1);
        }

        string rdlcPath = args[0];
        string datasetName = args[1];
        string fileType = args[2].ToUpper();

        try {
            string? dataFilePath = null;
            JsonObject? parameters = null;

            await using (Stream stdin = Console.OpenStandardInput()) {
                JsonNode? root = await JsonNode.ParseAsync(stdin);
                if (root == null) {
                    throw new Exception("Data JSON Wrapper Kosong");
                }

                dataFilePath = root["DataFilePath"]?.ToString();
                parameters = root["Parameters"]?.AsObject();
            }

            if (string.IsNullOrEmpty(dataFilePath) || !File.Exists(dataFilePath)) {
                throw new FileNotFoundException($"File Data Tidak Ditemukan: {dataFilePath}");
            }

            var dt = new DataTable(datasetName);
            await using (FileStream fs = File.OpenRead(dataFilePath)) {
                IAsyncEnumerable<JsonElement> dataStream = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fs);

                bool columnsCreated = false;
                await foreach (JsonElement element in dataStream) {
                    if (!columnsCreated) {
                        foreach (JsonProperty prop in element.EnumerateObject()) {
                            Type colType = GetTypeFromJsonKind(prop.Value);
                            _ = dt.Columns.Add(prop.Name, colType);
                        }

                        columnsCreated = true;
                    }

                    DataRow dr = dt.NewRow();
                    foreach (JsonProperty prop in element.EnumerateObject()) {
                        if (dt.Columns.Contains(prop.Name)) {
                            dr[prop.Name] = GetValueFromJsonElement(prop.Value);
                        }
                    }

                    dt.Rows.Add(dr);
                }
            }

            using (var report = new LocalReport()) {
                string? width = null;
                string? height = null;
                string? topMargin = null;
                string? bottomMargin = null;
                string? leftMargin = null;
                string? rightMargin = null;

                byte[] rdlcBytes = await File.ReadAllBytesAsync(rdlcPath);
                await using (var ms = new MemoryStream(rdlcBytes)) {
                    var xdoc = XDocument.Load(ms);
                    XNamespace ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

                    XElement? pageElement = xdoc.Descendants(ns + "Page").FirstOrDefault();
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

                await using (var rdlcStream = new MemoryStream(rdlcBytes)) {
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
                            throw new Exception("Format Tidak Tersedia");
                    }

                    var model = new RdlcReport() {
                        DisplayName = report.DisplayName,
                        RenderType = format
                    };

                    if (fileType == "PDF" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        model.RenderType = "HTML5";
                        string rawHtml = Encoding.UTF8.GetString(report.Render(model.RenderType));

                        double? wIn = ParseDimensionToInch(width);
                        double? hIn = ParseDimensionToInch(height);

                        if (wIn == null || hIn == null) {
                            throw new Exception($"Ukuran wIn ({wIn}) / hIn ({hIn}) Masih NULL");
                        }

                        double mTop = ParseDimensionToInch(topMargin) ?? 1.0;
                        double mBottom = ParseDimensionToInch(bottomMargin) ?? 1.0;
                        double mLeft = ParseDimensionToInch(leftMargin) ?? 1.0;
                        double mRight = ParseDimensionToInch(rightMargin) ?? 1.0;

                        string wStr = wIn?.ToString("0.###", CultureInfo.InvariantCulture) + "in";
                        string hStr = hIn?.ToString("0.###", CultureInfo.InvariantCulture) + "in";

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

                        var htmlToPdfDocument = new HtmlToPdfDocument() {
                            GlobalSettings = new GlobalSettings() {
                                DocumentTitle = model.DisplayName,
                                ColorMode = ColorMode.Color,
                                Margins = new MarginSettings() {
                                    Top = mTop,
                                    Bottom = mBottom,
                                    Left = mLeft,
                                    Right = mRight,
                                    Unit = Unit.Inches
                                },
                                PaperSize = new PechkinPaperSize(wStr, hStr),
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

                        using (var converter = new SynchronizedConverter(new PdfTools())) {
                            model.Report = converter.Convert(htmlToPdfDocument);
                        }
                    }
                    else {
                        model.Report = report.Render(model.RenderType);
                    }

                    byte[] reportData = model.Report;

                    await using (Stream stdout = Console.OpenStandardOutput()) {
                        await stdout.WriteAsync(reportData, 0, reportData.Length);
                        await stdout.FlushAsync();
                    }
                }
            }

            if (File.Exists(dataFilePath)) {
                File.Delete(dataFilePath);
            }

            await Task.Delay(100);

            Environment.Exit(0);
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync(ex.ToString());
            Environment.Exit(1);
        }
    }

}