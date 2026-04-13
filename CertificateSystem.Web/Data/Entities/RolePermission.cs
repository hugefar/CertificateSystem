using CertificateSystem.Web.Identity;

namespace CertificateSystem.Web.Data.Entities
{
    public class RolePermission
    {
        public long RoleId { get; set; }

        public long PermissionId { get; set; }

        public ApplicationRole Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;
    }
}
