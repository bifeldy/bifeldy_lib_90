namespace bifeldy_lib_90.Models {

    public sealed class CFtpResultInfo {
        public List<CFtpResultSendGet> Success { get; } = [];
        public List<CFtpResultSendGet> Fail { get; } = [];
    }

    public sealed class CFtpResultSendGet {
        public bool FtpStatusSendGet { get; set; }
        public FileInfo FileInformation { get; set; }
    }

}