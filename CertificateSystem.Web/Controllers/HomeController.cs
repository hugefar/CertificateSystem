using CertificateSystem.Web.Models;
using CertificateSystem.BLL;
using CertificateSystem.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace CertificateSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardService _dashboardService;
        private readonly IStudentSyncService _studentSyncService;
        private readonly ILogService _logService;

        public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService, IStudentSyncService studentSyncService, ILogService logService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
            _studentSyncService = studentSyncService;
            _logService = logService;
        }

        [PermissionAuthorize("Dashboard.View")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        [PermissionAuthorize("Dashboard.View")]
        public async Task<IActionResult> Dashboard(int? year, string? department)
        {
            var data = await _dashboardService.GetDashboardDataAsync(year, department);

            var vm = new DashboardViewModel
            {
                SelectedYear = year,
                SelectedDepartment = department,
                Years = data.Years,
                Departments = data.Departments,
                TotalPrintCount = data.Summary.TotalPrintCount,
                ActiveYearPrintCount = data.Summary.ActiveYearPrintCount,
                DepartmentCount = data.Summary.DepartmentCount,
                YearCount = data.Summary.YearCount
            };

            return View(vm);
        }

        [HttpGet]
        [PermissionAuthorize("Dashboard.View")]
        public async Task<IActionResult> GetChartData(int? year, string? department)
        {
            var points = await _dashboardService.GetChartDataAsync(year, department);
            return Json(new
            {
                labels = points.Select(x => x.Label),
                values = points.Select(x => x.Value)
            });
        }

        [HttpPost]
        [PermissionAuthorize("Dashboard.View")]
        public async Task<IActionResult> SyncStudentCertificates()
        {
            var result = await _studentSyncService.SyncAsync();
            await _logService.LogAsync(
                result.Success ? "手动同步" : "手动同步失败",
                "学生证书同步",
                result.Success
                    ? $"手动触发学生证书同步成功，共 {result.TotalRecords} 条。"
                    : $"手动触发学生证书同步失败：{result.Message}",
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                User.Identity?.Name ?? string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Json(new { success = result.Success, message = result.Message, totalRecords = result.TotalRecords });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
