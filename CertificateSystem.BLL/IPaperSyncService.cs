using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface IPaperSyncService
    {
        Task<SyncResult> SyncPapersAsync(CancellationToken cancellationToken = default);
    }
}
