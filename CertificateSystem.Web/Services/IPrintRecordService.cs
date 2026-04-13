using CertificateSystem.Model;

namespace CertificateSystem.Web.Services
{
    public interface IPrintRecordService
    {
        Task SaveActualPrintRecordAsync(StudentCertificate certificate, string certificateType, string operatorUserId, string? remark = null, CancellationToken cancellationToken = default);
        Task SaveActualPrintRecordsAsync(IEnumerable<StudentCertificate> certificates, string certificateType, string operatorUserId, string? remark = null, CancellationToken cancellationToken = default);
    }
}