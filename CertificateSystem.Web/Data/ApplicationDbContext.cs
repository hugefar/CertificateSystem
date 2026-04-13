using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CertificateSystem.Web.Identity;
using CertificateSystem.Web.Data.Entities;

namespace CertificateSystem.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");
                entity.HasKey(x => new { x.RoleId, x.PermissionId });

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Permission)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Permission>().HasData(
                new Permission { Id = 1, Code = "Dashboard.View", Name = "查看首页", Category = "系统首页", Description = "允许访问首页仪表盘" },
                new Permission { Id = 2, Code = "Certificate.Graduation", Name = "毕业证书管理", Category = "证书管理", Description = "毕业证书的查询和维护" },
                new Permission { Id = 3, Code = "Certificate.Completion", Name = "结业证书管理", Category = "证书管理", Description = "结业证书的查询和维护" },
                new Permission { Id = 4, Code = "Certificate.Degree", Name = "学位证书管理", Category = "证书管理", Description = "学位证书的查询和维护" },
                new Permission { Id = 5, Code = "Certificate.SecondDegree", Name = "第二学位证书管理", Category = "证书管理", Description = "第二学位证书的查询和维护" },
                new Permission { Id = 6, Code = "User.View", Name = "查看用户", Category = "用户管理", Description = "查看用户列表" },
                new Permission { Id = 7, Code = "User.Manage", Name = "管理用户", Category = "用户管理", Description = "新增、编辑、禁用用户" },
                new Permission { Id = 8, Code = "Role.View", Name = "查看角色", Category = "角色管理", Description = "查看角色列表" },
                new Permission { Id = 9, Code = "Role.Manage", Name = "管理角色", Category = "角色管理", Description = "新增、编辑、删除角色" },
                new Permission { Id = 10, Code = "Role.Permission", Name = "分配权限", Category = "角色管理", Description = "为角色分配功能权限" },
                new Permission { Id = 11, Code = "SecurityLog.View", Name = "查看安全日志", Category = "安全日志", Description = "查看安全审计日志" }
            );
        }
    }
}
