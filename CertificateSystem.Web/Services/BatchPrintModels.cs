using CertificateSystem.Model;

namespace CertificateSystem.Web.Services
{
    public enum BatchPrintMode
    {
        All = 0,
        Selected = 1
    }

    public enum BatchPrintTaskStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }

    public class BatchPrintTaskRequest
    {
        public string CertificateTypeKey { get; set; } = string.Empty;
        public string CertificateTypeName { get; set; } = string.Empty;
        public BatchPrintMode Mode { get; set; } = BatchPrintMode.All;
        public List<long> SelectedIds { get; set; } = new();
        public StudentCertificateQueryDto Filter { get; set; } = new();
        public string? OperatorName { get; set; }
    }

    public class BatchPrintTaskSnapshot
    {
        public string TaskId { get; set; } = string.Empty;
        public string CertificateTypeKey { get; set; } = string.Empty;
        public string CertificateTypeName { get; set; } = string.Empty;
        public BatchPrintTaskStatus Status { get; set; }
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public string? Message { get; set; }
        public string? ResultFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? OperatorName { get; set; }
        public DateTime? PrintedAt { get; set; }
        public List<BatchPrintCertificateItem> Certificates { get; set; } = new();
    }

    public class BatchPrintCertificateItem
    {
        public long CertificateId { get; set; }
        public string Institute { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class BatchPrintQueuedItem
    {
        public string TaskId { get; set; } = string.Empty;
        public BatchPrintTaskRequest Request { get; set; } = new();
    }
}
