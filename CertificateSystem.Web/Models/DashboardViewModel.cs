namespace CertificateSystem.Web.Models
{
    public class DashboardViewModel
    {
        public int? SelectedYear { get; set; }
        public string? SelectedDepartment { get; set; }

        public List<int> Years { get; set; } = new();
        public List<string> Departments { get; set; } = new();

        public int TotalPrintCount { get; set; }
        public int ActiveYearPrintCount { get; set; }
        public int DepartmentCount { get; set; }
        public int YearCount { get; set; }
    }
}
