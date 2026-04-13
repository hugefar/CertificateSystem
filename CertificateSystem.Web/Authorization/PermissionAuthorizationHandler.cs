using System.Security.Claims;
using CertificateSystem.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CertificateSystem.Web.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public PermissionAuthorizationHandler(ApplicationDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return;

            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
                return;

            var cacheKey = $"permission_codes_{userId}";
            if (!_cache.TryGetValue(cacheKey, out HashSet<string>? permissionCodes))
            {
                var roleIds = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                var codes = await _dbContext.RolePermissions
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Join(_dbContext.Permissions,
                        rp => rp.PermissionId,
                        p => p.Id,
                        (rp, p) => p.Code)
                    .Distinct()
                    .ToListAsync();

                permissionCodes = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
                _cache.Set(cacheKey, permissionCodes, TimeSpan.FromMinutes(2));
            }

            if (permissionCodes.Contains(requirement.PermissionCode))
            {
                context.Succeed(requirement);
            }
        }
    }
}
