using System.Reflection;

namespace bifeldy_lib_90.Models {

    // Kaga Bisa Pakai Inheritance Kalau ENUM -- Sue

    public abstract class ApiDocumentName {

        public static readonly string DEFAULT = "latest-" + Assembly.GetEntryAssembly().GetName().Version?.ToString().Replace(".", string.Empty);

        public static readonly string API_SD_1 = "Api-SD-1";
        public static readonly string API_SD_2 = "Api-SD-2";
        public static readonly string API_SD_3 = "Api-SD-3";
        public static readonly string API_SD_4 = "Api-SD-4";
        public static readonly string API_SD_5 = "Api-SD-5";
        public static readonly string API_SD_6 = "Api-SD-6";
        public static readonly string API_SD_7 = "Api-SD-7";

        public static readonly List<string> ApiDefaultDocuments = [
            DEFAULT,
            API_SD_1, API_SD_2, API_SD_3, API_SD_4, API_SD_5, API_SD_6, API_SD_7
        ];

        public static readonly string _ALL_ = "_ALL_";

    }

}
