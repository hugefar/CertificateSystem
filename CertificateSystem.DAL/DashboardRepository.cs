using System.Data;
using CertificateSystem.DBUtility;
using CertificateSystem.Model;
using System.Data.SqlClient;

namespace CertificateSystem.DAL
{
    public interface IDashboardRepository
    {
        Task<DashboardDataDto> GetDashboardDataAsync(int? year, string? department);
        Task<List<DashboardChartPoint>> GetChartDataAsync(int? year, string? department);
    }

    public class DashboardRepository : IDashboardRepository
    {
        public Task<DashboardDataDto> GetDashboardDataAsync(int? year, string? department)
        {
            var years = GetYears();
            var departments = GetDepartments();
            var summary = GetSummary(year, department);

            return Task.FromResult(new DashboardDataDto
            {
                Years = years,
                Departments = departments,
                Summary = summary
            });
        }

        public Task<List<DashboardChartPoint>> GetChartDataAsync(int? year, string? department)
        {
            var chart = string.IsNullOrWhiteSpace(department)
                ? GetDepartmentChartByYear(year)
                : GetYearChartByDepartment(department);

            return Task.FromResult(chart);
        }

        private List<int> GetYears()
        {
            const string sql = @"
SELECT DISTINCT YEAR(PrintTime) AS [Year]
FROM PrintRecords
WHERE PrintTime IS NOT NULL
ORDER BY [Year] DESC";

            var dt = SqlHelper.ExecuteDataTable(sql, CommandType.Text);
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["Year"])).ToList();
        }

        private List<string> GetDepartments()
        {
            const string sql = @"
SELECT DISTINCT Department
FROM PrintRecords
WHERE ISNULL(Department,'') <> ''
ORDER BY Department";

            var dt = SqlHelper.ExecuteDataTable(sql, CommandType.Text);
            return dt.Rows.Cast<DataRow>().Select(r => r["Department"].ToString() ?? string.Empty).ToList();
        }

        private DashboardSummaryDto GetSummary(int? year, string? department)
        {
            const string sql = @"
SELECT
    COUNT(1) AS TotalPrintCount,
    SUM(CASE WHEN YEAR(PrintTime) = ISNULL(@Year, YEAR(GETDATE())) THEN 1 ELSE 0 END) AS ActiveYearPrintCount,
    COUNT(DISTINCT Department) AS DepartmentCount,
    COUNT(DISTINCT YEAR(PrintTime)) AS YearCount
FROM PrintRecords
WHERE (@Department IS NULL OR @Department = '' OR Department = @Department)";

            var dt = SqlHelper.ExecuteDataTable(sql, CommandType.Text,
                new SqlParameter("@Year", (object?)year ?? DBNull.Value),
                new SqlParameter("@Department", (object?)department ?? DBNull.Value));

            if (dt.Rows.Count == 0)
                return new DashboardSummaryDto();

            var row = dt.Rows[0];
            return new DashboardSummaryDto
            {
                TotalPrintCount = row["TotalPrintCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["TotalPrintCount"]),
                ActiveYearPrintCount = row["ActiveYearPrintCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["ActiveYearPrintCount"]),
                DepartmentCount = row["DepartmentCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["DepartmentCount"]),
                YearCount = row["YearCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["YearCount"])
            };
        }

        private List<DashboardChartPoint> GetDepartmentChartByYear(int? year)
        {
            const string sql = @"
SELECT Department AS Label, COUNT(1) AS Value
FROM PrintRecords
WHERE (@Year IS NULL OR YEAR(PrintTime) = @Year)
GROUP BY Department
ORDER BY Value DESC";

            var dt = SqlHelper.ExecuteDataTable(sql, CommandType.Text,
                new SqlParameter("@Year", (object?)year ?? DBNull.Value));

            return dt.Rows.Cast<DataRow>()
                .Select(r => new DashboardChartPoint
                {
                    Label = r["Label"].ToString() ?? string.Empty,
                    Value = Convert.ToInt32(r["Value"])
                }).ToList();
        }

        private List<DashboardChartPoint> GetYearChartByDepartment(string department)
        {
            const string sql = @"
SELECT CAST(YEAR(PrintTime) AS NVARCHAR(10)) AS Label, COUNT(1) AS Value
FROM PrintRecords
WHERE Department = @Department
GROUP BY YEAR(PrintTime)
ORDER BY YEAR(PrintTime)";

            var dt = SqlHelper.ExecuteDataTable(sql, CommandType.Text,
                new SqlParameter("@Department", department));

            return dt.Rows.Cast<DataRow>()
                .Select(r => new DashboardChartPoint
                {
                    Label = r["Label"].ToString() ?? string.Empty,
                    Value = Convert.ToInt32(r["Value"])
                }).ToList();
        }
    }
}
