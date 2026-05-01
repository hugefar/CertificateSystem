using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportGraduationExcelAsync(StudentCertificateQueryDto filter);
        Task<byte[]> ExportCompletionExcelAsync(StudentCertificateQueryDto filter);
        Task<byte[]> ExportDegreeExcelAsync(StudentCertificateQueryDto filter);
        Task<byte[]> ExportSecondDegreeExcelAsync(StudentCertificateQueryDto filter);
    }
}
