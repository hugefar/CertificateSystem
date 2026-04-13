namespace CertificateSystem.Web.Services
{
    public interface ICertificateGenerator
    {
        Task<byte[]> GeneratePdfAsync(long id, string certificateType);
    }
}
