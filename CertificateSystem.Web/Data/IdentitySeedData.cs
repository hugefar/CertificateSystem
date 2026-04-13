using CertificateSystem.Web.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CertificateSystem.Web.Data
{
    public static class IdentitySeedData
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var roles = new[] { "Admin", "Operator" };
            foreach (var roleName in roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var role = new ApplicationRole { Name = roleName, DataScope = roleName == "Admin" ? "All" : "Institute" };
                    await roleManager.CreateAsync(role);
                }
            }

            var adminUserName = "admin";
            var adminEmail = "admin@cert.local";
            var adminPassword = "Admin@123456";

            var admin = await userManager.FindByNameAsync(adminUserName);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "系统管理员",
                    JobNum = "A0001",
                    Department = "教务处",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Seed admin user failed: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            var adminRole = await roleManager.FindByNameAsync("Admin");
            var securityLogPermission = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Code == "SecurityLog.View");
            if (adminRole != null && securityLogPermission != null)
            {
                var exists = await dbContext.RolePermissions.AnyAsync(x => x.RoleId == adminRole.Id && x.PermissionId == securityLogPermission.Id);
                if (!exists)
                {
                    dbContext.RolePermissions.Add(new Entities.RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = securityLogPermission.Id
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            var testUserName = "operator";
            var testUser = await userManager.FindByNameAsync(testUserName);
            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = testUserName,
                    Email = "operator@cert.local",
                    EmailConfirmed = true,
                    FullName = "测试操作员",
                    JobNum = "O0001",
                    Department = "信息中心",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createTestResult = await userManager.CreateAsync(testUser, "Operator@123456");
                if (!createTestResult.Succeeded)
                {
                    var errors = string.Join("; ", createTestResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Seed operator user failed: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(testUser, "Operator"))
            {
                await userManager.AddToRoleAsync(testUser, "Operator");
            }
        }
    }
}
