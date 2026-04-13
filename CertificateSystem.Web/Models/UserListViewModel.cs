using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CertificateSystem.Web.Models
{
    public class UserListItemViewModel
    {
        public long Id { get; set; }
        public string? JobNum { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string Roles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserListViewModel
    {
        public string? JobNum { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string? Status { get; set; }

        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        public List<UserListItemViewModel> Users { get; set; } = new();
    }
}
