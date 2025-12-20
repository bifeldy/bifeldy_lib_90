using WkHtmlToPdfDotNet;

namespace bifeldy_lib_90.Models {

    public sealed class RdlcInfo {
        public string contentType { get; set; }
        public string saveType { get; set; }
    }

    public sealed class RdlcReport {
        public byte[] Report { get; set; }
        public string HtmlContent { get; set; }
        public string RenderType { get; set; }
        public string DisplayName { get; set; }
        public PaperKind? PaperType { get; set; } = PaperKind.Custom;
        public MarginSettings Margins { get; set; } = new() { Top = 1, Bottom = 1, Left = 1, Right = 1, Unit = Unit.Centimeters };
        public Orientation? PageOrientation { get; set; } = Orientation.Portrait;
    }

}