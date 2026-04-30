using CertificateSystem.Model;

namespace CertificateSystem.DAL
{
    public interface IOraclePaperRepository
    {
        Task<List<OraclePaperRawDto>> GetAllPapersAsync(CancellationToken cancellationToken = default);
    }
}
