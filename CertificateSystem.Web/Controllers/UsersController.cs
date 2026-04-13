using CertificateSystem.Web.Identity;
using CertificateSystem.Web.Models;
using CertificateSystem.Web.Authorization;
using CertificateSystem.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CertificateSystem.Web.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogService _logService;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogService logService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logService = logService;
        }

        [HttpGet]
        [PermissionAuthorize("User.View")]
        public async Task<IActionResult> Index(string? jobNum, string? fullName, string? department, string? status)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(jobNum))
                query = query.Where(x => x.JobNum != null && x.JobNum.Contains(jobNum));

            if (!string.IsNullOrWhiteSpace(fullName))
                query = query.Where(x => x.FullName != null && x.FullName.Contains(fullName));

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(x => x.Department == department);

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.IsActive);
            else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => !x.IsActive);

            var users = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            var userItems = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userItems.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    JobNum = user.JobNum,
                    FullName = user.FullName,
                    Department = user.Department,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = string.Join("、", roles)
                });
            }

            var departments = await _userManager.Users
                .Where(x => !string.IsNullOrEmpty(x.Department))
                .Select(x => x.Department!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var vm = new UserListViewModel
            {
                JobNum = jobNum,
                FullName = fullName,
                Department = department,
                Status = status,
                Users = userItems,
                DepartmentOptions = departments.Select(x => new SelectListItem { Value = x, Text = x }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        [PermissionAuthorize("User.Manage")]
        public async Task<IActionResult> Create()
        {
            var vm = new CreateUserViewModel();
            await PopulateRoleOptionsAsync(vm.AvailableRoles);
            return View(vm);
        }

        [HttpPost]
        [PermissionAuthorize("User.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (await _userManager.Users.AnyAsync(x => x.JobNum == model.JobNum))
                ModelState.AddModelError(nameof(model.JobNum), "该职工号已存在");

            if (!ModelState.IsValid)
            {
                await PopulateRoleOptionsAsync(model.AvailableRoles);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.JobNum,
                JobNum = model.JobNum,
                FullName = model.FullName,
                Department = model.Department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                await PopulateRoleOptionsAsync(model.AvailableRoles);
                return View(model);
            }

            if (model.RoleIds.Any())
            {
                var roleNames = await _roleManager.Roles
                    .Where(r => model.RoleIds.Contains(r.Id))
                    .Select(r => r.Name!)
                    .ToListAsync();

                if (roleNames.Any())
                    await _userManager.AddToRolesAsync(user, roleNames);
            }

            TempData["SuccessMessage"] = "新增用户成功。";
            await LogAsync("创建用户", "用户管理", $"创建用户：{user.UserName}（{user.FullName}）");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [PermissionAuthorize("User.Manage")]
        public async Task<IActionResult> Edit(long id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "用户不存在。";
                return RedirectToAction(nameof(Index));
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var roleIds = await _roleManager.Roles
                .Where(r => r.Name != null && userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                JobNum = user.JobNum ?? user.UserName ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Department = user.Department ?? string.Empty,
                IsActive = user.IsActive,
                RoleIds = roleIds
            };

            await PopulateRoleOptionsAsync(vm.AvailableRoles);
            return View(vm);
        }

        [HttpPost]
        [PermissionAuthorize("User.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRoleOptionsAsync(model.AvailableRoles);
                return View(model);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "用户不存在。";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = model.FullName;
            user.Department = model.Department;
            user.IsActive = model.IsActive;
            user.LockoutEnabled = true;
            user.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                await PopulateRoleOptionsAsync(model.AvailableRoles);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var targetRoles = await _roleManager.Roles
                .Where(r => model.RoleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync();

            var toAdd = targetRoles.Except(currentRoles).ToList();
            var toRemove = currentRoles.Except(targetRoles).ToList();

            if (toRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (toAdd.Any())
                await _userManager.AddToRolesAsync(user, toAdd);

            TempData["SuccessMessage"] = "用户信息更新成功。";
            await LogAsync("编辑用户", "用户管理", $"编辑用户：{user.UserName}（{user.FullName}）");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [PermissionAuthorize("User.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "用户不存在。";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "用户已禁用（软删除）。"
                : "删除失败，请稍后重试。";

            if (result.Succeeded)
                await LogAsync("删除用户", "用户管理", $"禁用用户：{user.UserName}（{user.FullName}）");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [PermissionAuthorize("User.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "用户不存在。";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            user.LockoutEnabled = true;
            user.LockoutEnd = user.IsActive ? null : DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? (user.IsActive ? "用户已启用。" : "用户已禁用。")
                : "状态切换失败，请稍后重试。";

            if (result.Succeeded)
                await LogAsync(user.IsActive ? "启用用户" : "禁用用户", "用户管理", $"{(user.IsActive ? "启用" : "禁用")}用户：{user.UserName}（{user.FullName}）");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [PermissionAuthorize("User.Manage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(long id, string? newPassword)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "用户不存在。";
                return RedirectToAction(nameof(Index));
            }

            var password = string.IsNullOrWhiteSpace(newPassword) ? "Admin@123456" : newPassword;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"密码已重置成功。新密码：{password}";
                await LogAsync("重置密码", "用户管理", $"重置用户密码：{user.UserName}（{user.FullName}）");
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("；", result.Errors.Select(x => x.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateRoleOptionsAsync(List<SelectListItem> target)
        {
            var roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync();
            target.Clear();
            target.AddRange(roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }));
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
