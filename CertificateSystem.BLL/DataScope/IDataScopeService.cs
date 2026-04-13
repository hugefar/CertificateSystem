namespace CertificateSystem.BLL.DataScope
{
    public interface IDataScopeService
    {
        Task<string?> GetCurrentUserInstituteAsync();
        Task<bool> CanAccessAllInstitutesAsync();
    }
}
