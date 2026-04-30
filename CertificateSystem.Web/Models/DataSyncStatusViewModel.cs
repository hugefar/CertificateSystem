namespace CertificateSystem.Web.Models
{
    public class DataSyncItemStatusViewModel
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? LastSyncTime { get; set; }
        public int LastSyncCount { get; set; }
        public string Status { get; set; } = "未执行";
        public string Message { get; set; } = string.Empty;
    }

    public class DataSyncStatusViewModel
    {
        public DataSyncItemStatusViewModel CertificateSync { get; set; } = new();
        public DataSyncItemStatusViewModel PaperSync { get; set; } = new();
    }
}
