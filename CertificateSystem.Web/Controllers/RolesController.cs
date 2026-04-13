using CertificateSystem.Web.Data;
using CertificateSystem.Web.Data.Entities;
using CertificateSystem.Web.Authorization;
using CertificateSystem.Web.Identity;
using CertificateSystem.Web.Models;
using CertificateSystem.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CertificateSystem.Web.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogService _logService;

        public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILogService logService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _dbContext = dbContext;
            _logService = logService;
        }

        [HttpGet]
        [PermissionAuthorize("Role.View")]
        public async Task<IActionResult> Index(string? keyword)
        {
            var rolesQuery = _roleManager.Roles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                rolesQuery = rolesQuery.Where(r => r.Name != null && r.Name.Contains(keyword));
            }

            var roles = await rolesQuery.OrderBy(r => r.Name).ToListAsync();

            var roleIds = roles.Select(r => r.Id).ToList();
            var userRoleCounts = await _dbContext.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

            var permissionCounts = await _dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .GroupBy(rp => rp.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

            var vm = new RoleListViewModel
            {
                Keyword = keyword,
                Roles = roles.Select(r => new RoleListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    UserCount = userRoleCounts.TryGetValue(r.Id, out var uc) ? uc : 0,
                    PermissionCount = permissionCounts.TryGetValue(r.Id, out var pc) ? pc : 0
                    ,
                    DataScope = r.DataScope
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        [PermissionAuthorize("Role.Manage")]
        public IActionResult Create()
        {
            return View(new CreateRoleViewModel());
        }

        [HttpPost]
        [PermissionAuthorize("Role.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "角色名称已存在");
                return View(model);
            }

            var role = new ApplicationRole { Name = model.Name, DataScope = model.DataScope };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            TempData["SuccessMessage"] = "角色创建成功。";
            await LogAsync("创建角色", "角色管理", $"创建角色：{role.Name}");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [PermissionAuthorize("Role.Manage")]
        public async Task<IActionResult> Edit(long id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            return View(new EditRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                DataScope = role.DataScope ?? "Institute"
            });
        }

        [HttpPost]
        [PermissionAuthorize("Role.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var role = await _roleManager.FindByIdAsync(model.Id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _roleManager.Roles.AnyAsync(r => r.Id != model.Id && r.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "角色名称已存在");
                return View(model);
            }

            role.Name = model.Name;
            role.DataScope = model.DataScope;
            role.NormalizedName = model.Name.ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            TempData["SuccessMessage"] = "角色更新成功。";
            await LogAsync("编辑角色", "角色管理", $"编辑角色：{role.Name}");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [PermissionAuthorize("Role.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            if (await HasUsersInRoleAsync(role.Name!))
            {
                TempData["ErrorMessage"] = "该角色已分配给用户，无法删除。";
                return RedirectToAction(nameof(Index));
            }

            var rolePermissions = await _dbContext.RolePermissions.Where(x => x.RoleId == id).ToListAsync();
            if (rolePermissions.Count > 0)
            {
                _dbContext.RolePermissions.RemoveRange(rolePermissions);
                await _dbContext.SaveChangesAsync();
            }

            var result = await _roleManager.DeleteAsync(role);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "角色删除成功。"
                : string.Join("；", result.Errors.Select(x => x.Description));

            if (result.Succeeded)
                await LogAsync("删除角色", "角色管理", $"删除角色：{role.Name}");

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [PermissionAuthorize("Role.Permission")]
        public async Task<IActionResult> AssignPermissions(long id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            var selectedPermissionIds = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var permissions = await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var groups = permissions
                .GroupBy(p => p.Category)
                .Select(g => new PermissionGroupViewModel
                {
                    Category = g.Key,
                    Permissions = g.Select(p => new PermissionItemViewModel
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        IsSelected = selectedPermissionIds.Contains(p.Id)
                    }).ToList()
                })
                .ToList();

            var vm = new RolePermissionViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                PermissionIds = selectedPermissionIds,
                Groups = groups
            };

            return View(vm);
        }

        [HttpPost]
        [PermissionAuthorize("Role.Permission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPermissions(RolePermissionViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            var selectedIds = (model.PermissionIds ?? new List<long>()).Distinct().ToList();

            var existing = await _dbContext.RolePermissions
                .Where(x => x.RoleId == model.RoleId)
                .ToListAsync();

            _dbContext.RolePermissions.RemoveRange(existing);

            if (selectedIds.Count > 0)
            {
                var validPermissionIds = await _dbContext.Permissions
                    .Where(p => selectedIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var newMappings = validPermissionIds
                    .Select(pid => new RolePermission
                    {
                        RoleId = model.RoleId,
                        PermissionId = pid
                    })
                    .ToList();

                await _dbContext.RolePermissions.AddRangeAsync(newMappings);
            }

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "角色权限分配成功。";
            await LogAsync("分配权限", "角色管理", $"为角色 {role.Name} 分配权限，共 {selectedIds.Count} 项");
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> HasUsersInRoleAsync(string roleName)
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task LogAsync(string operationType, string module, string content)
        {
            await _logService.LogAsync(
                operationType,
                module,
                content,
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                User.Identity?.Name ?? string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        }
    }
}
