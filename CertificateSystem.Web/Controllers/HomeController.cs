using CertificateSystem.Web.Models;
using CertificateSystem.BLL;
using CertificateSystem.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CertificateSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardService _dashboardService;

        public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
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
