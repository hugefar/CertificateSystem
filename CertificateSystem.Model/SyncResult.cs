namespace CertificateSystem.Model
{
    public class SyncResult
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int InsertedCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
    }
}
