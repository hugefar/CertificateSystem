using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface IWordExportService
    {
        byte[] ExportDegreeWord(List<StudentCertificate> students);
        byte[] ExportSecondDegreeWord(List<StudentCertificate> students);
    }
}
