using CertificateSystem.BLL;
using CertificateSystem.Model;
using CertificateSystem.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CertificateSystem.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [PermissionAuthorize("SecurityLog.View")]
    public class SecurityLogController : Controller
    {
        private readonly ILogService _logService;

        public SecurityLogController(ILogService logService)
        {
            _logService = logService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.OperationTypes = BuildSelectList(new[]
            {
                "登录成功", "登录失败", "登出", "创建用户", "编辑用户", "删除用户", "启用用户", "禁用用户", "重置密码",
                "创建角色", "编辑角色", "删除角色", "分配权限", "证书预览", "证书生成", "证书打印", "批量生成任务创建", "批量打印"
            });

            ViewBag.OperationModules = BuildSelectList(new[]
            {
                "认证", "用户管理", "角色管理", "毕业证书", "结业证书", "学位证书", "第二学位证书", "安全日志"
            });

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetList([FromBody] SecurityLogQueryDto query)
        {
            var data = await _logService.GetPagedListAsync(query ?? new SecurityLogQueryDto());
            return Json(new { total = data.TotalCount, data = data.Items });
        }

        private static List<SelectListItem> BuildSelectList(IEnumerable<string> items)
        {
            return items.Select(x => new SelectListItem { Value = x, Text = x }).ToList();
        }
    }
}
