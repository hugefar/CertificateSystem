using CertificateSystem.BLL.DataScope;
using CertificateSystem.Web.Authorization;
using CertificateSystem.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CertificateSystem.Web.Services
{
    public class DataScopeService : IDataScopeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuthorizationService _authorizationService;

        public DataScopeService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _roleManager = roleManager;
            _authorizationService = authorizationService;
        }

        public async Task<string?> GetCurrentUserInstituteAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return null;

            // If user has All data scope via roles, return null (no institute restriction)
            var canAccessAll = await CanAccessAllInstitutesAsync();
            if (canAccessAll)
                return null;

            // Otherwise fallback to user's Department
            return user?.Department;
        }

        public async Task<bool> CanAccessAllInstitutesAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            // Get roles of the user and check role.DataScope == "All"
            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || roles.Count == 0)
                return false;

            foreach (var roleName in roles)
            {
                var r = await _roleManager.FindByNameAsync(roleName);
                if (r != null && string.Equals(r.DataScope, "All", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            return await _userManager.GetUserAsync(principal);
        }
    }
}
