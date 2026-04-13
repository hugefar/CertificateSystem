using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CertificateSystem.Web.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "请输入职工号")]
        [Display(Name = "职工号")]
        public string JobNum { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入真实姓名")]
        [Display(Name = "真实姓名")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入所属部门")]
        [Display(Name = "所属学院/部门")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度至少6位")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入确认密码")]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "角色分配")]
        public List<long> RoleIds { get; set; } = new();

        public List<SelectListItem> AvailableRoles { get; set; } = new();
    }
}
