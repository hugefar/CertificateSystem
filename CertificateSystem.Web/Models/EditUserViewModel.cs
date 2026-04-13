using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CertificateSystem.Web.Models
{
    public class EditUserViewModel
    {
        public long Id { get; set; }

        [Display(Name = "职工号")]
        public string JobNum { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入真实姓名")]
        [Display(Name = "真实姓名")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入所属部门")]
        [Display(Name = "所属学院/部门")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "是否启用")]
        public bool IsActive { get; set; }

        [Display(Name = "角色分配")]
        public List<long> RoleIds { get; set; } = new();

        public List<SelectListItem> AvailableRoles { get; set; } = new();
    }
}
