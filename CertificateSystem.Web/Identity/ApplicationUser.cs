using Microsoft.AspNetCore.Identity;

namespace CertificateSystem.Web.Identity
{
    public class ApplicationUser : IdentityUser<long>
    {
        public string? FullName { get; set; }

        public string? JobNum { get; set; }

        public string? Department { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginTime { get; set; }
    }
}
