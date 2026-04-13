using Microsoft.AspNetCore.Identity;
using CertificateSystem.Web.Data.Entities;

namespace CertificateSystem.Web.Identity
{
    public class ApplicationRole : IdentityRole<long>
    {
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        /// <summary>
        /// 数据范围：All=全部数据，Institute=本学院数据，Self=仅本人数据
        /// </summary>
        public string? DataScope { get; set; }
    }
}
