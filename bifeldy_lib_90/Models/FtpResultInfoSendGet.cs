namespace bifeldy_lib_90.Models {

    public sealed class CFtpResultInfo {
        public List<CFtpResultSendGet> Success { get; } = new List<CFtpResultSendGet>();
        public List<CFtpResultSendGet> Fail { get; } = new List<CFtpResultSendGet>();
    }

    public sealed class CFtpResultSendGet {
        public bool FtpStatusSendGet { get; set; }
        public FileInfo FileInformation { get; set; }
    }

}