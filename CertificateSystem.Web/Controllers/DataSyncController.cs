using System.Security.Claims;
using CertificateSystem.BLL;
using CertificateSystem.Model;
using CertificateSystem.Web.Authorization;
using CertificateSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertificateSystem.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [PermissionAuthorize("DataSync.View")]
    public class DataSyncController : Controller
    {
        private readonly IStudentSyncService _studentSyncService;
        private readonly IPaperSyncService _paperSyncService;
        private readonly ILogService _logService;

        public DataSyncController(IStudentSyncService studentSyncService, IPaperSyncService paperSyncService, ILogService logService)
        {
            _studentSyncService = studentSyncService;
            _paperSyncService = paperSyncService;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncCertificates()
        {
            var result = await _studentSyncService.SyncAsync();
            await WriteManualLogAsync(result, "学生证书同步");
            return Json(new { success = result.Success, message = result.Message, totalRecords = result.TotalRecords, insertedCount = result.InsertedCount, executedAt = result.ExecutedAt });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPapers()
        {
            var result = await _paperSyncService.SyncPapersAsync();
            await WriteManualLogAsync(result, "学生论文同步");
            return Json(new { success = result.Success, message = result.Message, totalRecords = result.TotalRecords, insertedCount = result.InsertedCount, executedAt = result.ExecutedAt });
        }

        [HttpGet]
        public async Task<IActionResult> GetSyncStatus()
        {
            var logs = await _logService.GetLatestByModulesAsync(new[] { "学生证书同步", "学生论文同步" }, 20);

            var certificateStatus = BuildStatus(logs, "学生证书同步", "学生证书同步");
            var paperStatus = BuildStatus(logs, "学生论文同步", "学生论文同步");

            var vm = new DataSyncStatusViewModel
            {
                CertificateSync = certificateStatus,
                PaperSync = paperStatus
            };

            return Json(vm);
        }

        private async Task WriteManualLogAsync(SyncResult result, string module)
        {
            await _logService.LogAsync(
                result.Success ? "手动同步" : "手动同步失败",
                module,
                result.Success
                    ? $"手动触发{module}成功，共 {result.TotalRecords} 条。"
                    : $"手动触发{module}失败：{result.Message}",
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                User.Identity?.Name ?? string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        }

        private static DataSyncItemStatusViewModel BuildStatus(IEnumerable<SecurityLog> logs, string module, string name)
        {
            var moduleLogs = logs
                .Where(x => string.Equals(x.OperationModule, module, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToList();

            var latest = moduleLogs.FirstOrDefault(x => x.OperationType is "同步完成" or "同步失败" or "手动同步" or "手动同步失败");
            if (latest == null)
            {
                return new DataSyncItemStatusViewModel
                {
                    Name = name,
                    Status = "未执行",
                    Message = "暂无同步记录"
                };
            }

            return new DataSyncItemStatusViewModel
            {
                Name = name,
                LastSyncTime = latest.CreatedAt,
                LastSyncCount = ExtractCount(latest.Content),
                Status = latest.OperationType.Contains("失败", StringComparison.OrdinalIgnoreCase) ? "失败" : "成功",
                Message = latest.Content
            };
        }

        private static int ExtractCount(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return 0;
            }

            var match = System.Text.RegularExpressions.Regex.Match(content, @"共\s*(\d+)\s*条");
            return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 0;
        }
    }
}
