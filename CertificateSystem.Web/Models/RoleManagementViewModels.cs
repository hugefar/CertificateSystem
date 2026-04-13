using System.ComponentModel.DataAnnotations;

namespace CertificateSystem.Web.Models
{
    public class RoleListItemViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public string? DataScope { get; set; }
    }

    public class RoleListViewModel
    {
        public string? Keyword { get; set; }
        public List<RoleListItemViewModel> Roles { get; set; } = new();
    }

    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "请输入角色名称")]
        [Display(Name = "角色名称")]
        public string Name { get; set; } = string.Empty;
        public string DataScope { get; set; } = "Institute";
    }

    public class EditRoleViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "请输入角色名称")]
        [Display(Name = "角色名称")]
        public string Name { get; set; } = string.Empty;
        public string DataScope { get; set; } = "Institute";
    }

    public class PermissionItemViewModel
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class PermissionGroupViewModel
    {
        public string Category { get; set; } = string.Empty;
        public List<PermissionItemViewModel> Permissions { get; set; } = new();
    }

    public class RolePermissionViewModel
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<long> PermissionIds { get; set; } = new();
        public List<PermissionGroupViewModel> Groups { get; set; } = new();
    }
}
