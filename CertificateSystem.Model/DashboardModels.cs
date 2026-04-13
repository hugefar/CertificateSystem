namespace CertificateSystem.Model
{
    public class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalPrintCount { get; set; }
        public int ActiveYearPrintCount { get; set; }
        public int DepartmentCount { get; set; }
        public int YearCount { get; set; }
    }

    public class DashboardDataDto
    {
        public List<int> Years { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public DashboardSummaryDto Summary { get; set; } = new();
    }
}
