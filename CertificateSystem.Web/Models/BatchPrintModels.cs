using CertificateSystem.Model;

namespace CertificateSystem.Web.Models
{
    public class BatchPreviewStartRequest
    {
        public string CertificateType { get; set; } = string.Empty;
        public string PrintMode { get; set; } = "All";
        public List<long> SelectedIds { get; set; } = new();
        public StudentCertificateQueryDto Filter { get; set; } = new();
    }

    public class BatchPreviewRecordRequest
    {
        public string TaskId { get; set; } = string.Empty;
        public string ActionType { get; set; } = "Print";
    }
}
