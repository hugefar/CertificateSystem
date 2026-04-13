using CertificateSystem.DAL;
using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface ILogService
    {
        Task LogAsync(string operationType, string module, string content, string userId, string userName, string ipAddress);
        Task<PagedResult<SecurityLog>> GetPagedListAsync(SecurityLogQueryDto query);
    }

    public class LogService : ILogService
    {
        private readonly ISecurityLogRepository _repository;

        public LogService(ISecurityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task LogAsync(string operationType, string module, string content, string userId, string userName, string ipAddress)
        {
            try
            {
                await _repository.InsertAsync(new SecurityLog
                {
                    OperationType = operationType,
                    OperationModule = module,
                    Content = content,
                    OperatorUserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                    OperatorName = string.IsNullOrWhiteSpace(userName) ? null : userName,
                    IPAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
                    CreatedAt = DateTime.Now
                });
            }
            catch
            {
            }
        }

        public async Task<PagedResult<SecurityLog>> GetPagedListAsync(SecurityLogQueryDto query)
        {
            return await _repository.GetPagedListAsync(query ?? new SecurityLogQueryDto());
        }
    }
}
