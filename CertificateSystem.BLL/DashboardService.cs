using CertificateSystem.DAL;
using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface IDashboardService
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int? year, string? department);
        Task<List<DashboardChartPoint>> GetChartDataAsync(int? year, string? department);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public Task<DashboardDataDto> GetDashboardDataAsync(int? year, string? department)
        {
            return _dashboardRepository.GetDashboardDataAsync(year, department);
        }

        public Task<List<DashboardChartPoint>> GetChartDataAsync(int? year, string? department)
        {
            return _dashboardRepository.GetChartDataAsync(year, department);
        }
    }
}
