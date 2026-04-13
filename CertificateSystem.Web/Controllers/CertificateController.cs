using CertificateSystem.BLL;
using CertificateSystem.Web.Authorization;
using CertificateSystem.Web.Models;
using CertificateSystem.Web.Services;
using CertificateSystem.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CertificateSystem.Web.Controllers
{
    [Authorize]
    [Route("Certificate")]
    public class CertificateController : Controller
    {
        private readonly ICertificateService _certificateService;
        private readonly ICertificateGenerator _certificateGenerator;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<CertificateController> _logger;
        private readonly IBatchPrintQueue _batchPrintQueue;
        private readonly IPrintRecordService _printRecordService;

        // Use the permission codes already seeded in the Permissions table (e.g. "Certificate.Graduation")
        // For now treat the same permission as both view and print to keep behavior consistent with seeded data.
        private static readonly Dictionary<string, (string TypeName, string ViewPermission, string PrintPermission)> TypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Graduation"] = ("毕业证书", "Certificate.Graduation", "Certificate.Graduation"),
                ["Completion"] = ("结业证书", "Certificate.Completion", "Certificate.Completion"),
                ["Degree"] = ("学位证书", "Certificate.Degree", "Certificate.Degree"),
                ["SecondDegree"] = ("第二学位证书", "Certificate.SecondDegree", "Certificate.SecondDegree")
            };

        public CertificateController(
            ICertificateService certificateService,
            ICertificateGenerator certificateGenerator,
            IAuthorizationService authorizationService,
            ILogger<CertificateController> logger,
            IBatchPrintQueue batchPrintQueue,
            IPrintRecordService printRecordService)
        {
            _certificateService = certificateService;
            _certificateGenerator = certificateGenerator;
            _authorizationService = authorizationService;
            _logger = logger;
            _batchPrintQueue = batchPrintQueue;
            _printRecordService = printRecordService;
        }

        [HttpGet("GetGraduationYears")]
        public async Task<IActionResult> GetGraduationYears(string certificateType)
        {
            if (!TryGetTypeConfig(certificateType, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var list = await _certificateService.GetDistinctGraduationYearsAsync(cfg.TypeName);
            // return as simple list of strings
            return Json(list);
        }

        [HttpGet("GetInstitutes")]
        public async Task<IActionResult> GetInstitutes(string certificateType, string graduationYear)
        {
            if (!TryGetTypeConfig(certificateType, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var list = await _certificateService.GetDistinctInstitutesAsync(cfg.TypeName, graduationYear);
            return Json(list);
        }

        [HttpGet("GetMajors")]
        public async Task<IActionResult> GetMajors(string certificateType, string graduationYear, string institute)
        {
            if (!TryGetTypeConfig(certificateType, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var list = await _certificateService.GetDistinctMajorsAsync(cfg.TypeName, graduationYear, institute);
            return Json(list);
        }

        [HttpGet("GetClasses")]
        public async Task<IActionResult> GetClasses(string certificateType, string graduationYear, string institute, string major)
        {
            if (!TryGetTypeConfig(certificateType, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var list = await _certificateService.GetDistinctClassesAsync(cfg.TypeName, graduationYear, institute, major);
            return Json(list);
        }

        [HttpGet("{type}")]
        public async Task<IActionResult> Index(string type)
        {
            if (!TryGetTypeConfig(type, out var cfg))
                return NotFound();

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.ViewPermission));
            if (!auth.Succeeded)
                return Forbid();

            ViewBag.Title = $"{cfg.TypeName}打印管理";
            ViewBag.CertificateType = type.ToLowerInvariant();
            ViewBag.CertificateTypeName = cfg.TypeName;
            return View("Index");
        }

        [HttpPost("GetList")]
        public async Task<IActionResult> GetList([FromBody] StudentCertificateQueryDto query)
        {
            query ??= new StudentCertificateQueryDto();

            var incomingType = query.CertificateType ?? string.Empty;
            if (!TryGetTypeConfig(incomingType, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.ViewPermission));
            if (!auth.Succeeded)
                return Forbid();

            query.CertificateType = cfg.TypeName; // Override with mapped type name to ensure correct filtering
            var data = await _certificateService.GetPagedListAsync(query);
            return Json(new { total = data.TotalCount, data = data.Items });
        }

        [HttpGet("{type}/Preview/{id:long}")]
        public async Task<IActionResult> Preview(string type, long id)
        {
            if (!TryGetTypeConfig(type, out var cfg))
                return NotFound();

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            var entity = await _certificateService.GetByIdAsync(id);
            var url = Url.Action(nameof(GetPdfStream), "Certificate", new { id = id, certificateType = type });
            return Json(new { success = true, url = url, name = entity?.Name ?? string.Empty });
        }

        [HttpGet("GetPdfStream/{id:long}")]
        public async Task<IActionResult> GetPdfStream(long id, string certificateType)
        {
            if (!TryGetTypeConfig(certificateType, out var cfg))
                return NotFound();

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            var entity = await _certificateService.GetByIdAsync(id);
            var bytes = await _certificateGenerator.GeneratePdfAsync(id, cfg.TypeName);
            var stream = new System.IO.MemoryStream(bytes ?? Array.Empty<byte>());

            var fileName = $"{cfg.TypeName}_{entity?.StudentId}_{entity?.Name}.pdf";
            Response.Headers["Content-Disposition"] = BuildInlineContentDisposition(fileName);
            Response.ContentLength = stream.Length;

            return new FileStreamResult(stream, "application/pdf");
        }

        [HttpPost("BatchPreview/Start")]
        public async Task<IActionResult> StartBatchPreview([FromBody] BatchPreviewStartRequest request)
        {
            if (request == null || !TryGetTypeConfig(request.CertificateType, out var cfg))
                return BadRequest(new { success = false, message = "无效的请求参数。" });

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            var mode = string.Equals(request.PrintMode, "Selected", StringComparison.OrdinalIgnoreCase)
                ? BatchPrintMode.Selected
                : BatchPrintMode.All;

            if (mode == BatchPrintMode.Selected && (request.SelectedIds == null || request.SelectedIds.Count == 0))
            {
                return BadRequest(new { success = false, message = "请先选择要打印的数据。" });
            }

            var filter = request.Filter ?? new StudentCertificateQueryDto();
            filter.CertificateType = cfg.TypeName;

            var taskId = _batchPrintQueue.Enqueue(new BatchPrintTaskRequest
            {
                CertificateTypeKey = request.CertificateType,
                CertificateTypeName = cfg.TypeName,
                Mode = mode,
                SelectedIds = request.SelectedIds ?? new List<long>(),
                Filter = filter,
                OperatorName = User.Identity?.Name
            });

            return Json(new { success = true, taskId, message = "批量任务已提交，正在后台处理。" });
        }

        [HttpGet("BatchPreview/Status/{taskId}")]
        public async Task<IActionResult> GetBatchPreviewStatus(string taskId)
        {
            var snapshot = _batchPrintQueue.GetSnapshot(taskId);
            if (snapshot == null)
                return NotFound(new { success = false, message = "任务不存在。" });

            if (!TryGetTypeConfig(snapshot.CertificateTypeKey, out var cfg))
                return BadRequest(new { success = false, message = "无效的证书类型。" });

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            var url = snapshot.Status == BatchPrintTaskStatus.Completed
                ? Url.Action(nameof(GetMergedPdfStream), "Certificate", new { taskId })
                : null;

            return Json(new
            {
                success = true,
                status = snapshot.Status.ToString(),
                total = snapshot.TotalCount,
                processed = snapshot.ProcessedCount,
                message = snapshot.Message,
                url
            });
        }

        [HttpGet("GetMergedPdfStream/{taskId}")]
        public async Task<IActionResult> GetMergedPdfStream(string taskId)
        {
            //
            var taskId0 = taskId;
            if (taskId.Contains("?_="))
            {
                taskId0=taskId.Split("?_=")[0];
            }
            var snapshot = _batchPrintQueue.GetSnapshot(taskId0);
            if (snapshot == null || snapshot.Status != BatchPrintTaskStatus.Completed || string.IsNullOrWhiteSpace(snapshot.ResultFilePath))
                return NotFound();

            if (!TryGetTypeConfig(snapshot.CertificateTypeKey, out var cfg))
                return BadRequest(new { message = "无效的证书类型。" });

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            if (!System.IO.File.Exists(snapshot.ResultFilePath))
                return NotFound(new { message = "合并文件不存在或已过期。" });

            var fs = new FileStream(snapshot.ResultFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var fileName = $"{snapshot.CertificateTypeName}_批量打印.pdf";
            Response.Headers["Content-Disposition"] = BuildInlineContentDisposition(fileName);
            return File(fs, "application/pdf", enableRangeProcessing: true);
        }

        [HttpPost("BatchPreview/Record")]
        public async Task<IActionResult> RecordBatchPreviewAction([FromBody] BatchPreviewRecordRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "参数无效。" });

            var actionType = string.Equals(request.ActionType, "Print", StringComparison.OrdinalIgnoreCase)
                ? "Print"
                : "Preview";

            if (!string.Equals(actionType, "Print", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = true });
            }

            var operatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(request.TaskId))
            {
                var snapshot = _batchPrintQueue.GetSnapshot(request.TaskId);
                if (snapshot == null)
                    return NotFound(new { success = false, message = "任务不存在。" });

                if (!TryGetTypeConfig(snapshot.CertificateTypeKey, out var cfg))
                    return BadRequest(new { success = false, message = "无效的证书类型。" });

                var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
                if (!auth.Succeeded)
                    return Forbid();

                if (snapshot.Certificates.Count > 0)
                {
                    var batchCertificates = snapshot.Certificates.Select(x => new StudentCertificate
                    {
                        Id = x.CertificateId,
                        Institute = x.Institute,
                        StudentId = x.StudentId,
                        Name = x.Name
                    }).ToList();

                    await _printRecordService.SaveActualPrintRecordsAsync(batchCertificates, snapshot.CertificateTypeName, operatorUserId, request.Remark ?? "批量打印");
                }

                _batchPrintQueue.MarkPrinted(request.TaskId);
                return Json(new { success = true });
            }

            if (!request.CertificateId.HasValue || string.IsNullOrWhiteSpace(request.CertificateType) || !TryGetTypeConfig(request.CertificateType, out var singleCfg))
                return BadRequest(new { success = false, message = "参数无效。" });

            var singleAuth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(singleCfg.PrintPermission));
            if (!singleAuth.Succeeded)
                return Forbid();

            var entity = await _certificateService.GetByIdAsync(request.CertificateId.Value);
            await _printRecordService.SaveActualPrintRecordAsync(entity, singleCfg.TypeName, operatorUserId, request.Remark ?? "单个打印");

            return Json(new { success = true });
        }

        [HttpPost("BatchPrint")]
        public async Task<IActionResult> BatchPrint([FromBody] BatchPrintRequestDto request)
        {
            if (request == null || !TryGetTypeConfig(request.CertificateType, out var cfg))
                return BadRequest(new { message = "无效的请求参数。" });

            var auth = await _authorizationService.AuthorizeAsync(User, null, new PermissionRequirement(cfg.PrintPermission));
            if (!auth.Succeeded)
                return Forbid();

            var filter = request.Filter ?? new StudentCertificateQueryDto();
            filter.CertificateType = cfg.TypeName;

            var result = await _certificateService.CreateBatchPrintTaskAsync(filter, cfg.TypeName, User.Identity?.Name);
            _logger.LogInformation("Batch print task created. TaskId={TaskId}, Type={Type}, Total={Total}", result.TaskId, cfg.TypeName, result.TotalCount);

            return Json(new
            {
                taskId = result.TaskId,
                totalCount = result.TotalCount,
                message = result.Message
            });
        }

        private static bool TryGetTypeConfig(string? type, out (string TypeName, string ViewPermission, string PrintPermission) config)
        {
            if (!string.IsNullOrWhiteSpace(type) && TypeMap.TryGetValue(type, out config))
                return true;

            config = default;
            return false;
        }

        private static string BuildInlineContentDisposition(string fileName)
        {
            var asciiFallback = new System.Text.StringBuilder();
            foreach (var ch in fileName)
            {
                asciiFallback.Append(ch <= 127 && ch != '"' ? ch : '_');
            }

            var encoded = Uri.EscapeDataString(fileName);
            return $"inline; filename=\"{asciiFallback}\"; filename*=UTF-8''{encoded}";
        }


    }
}
