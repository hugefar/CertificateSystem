using CertificateSystem.DAL;
using CertificateSystem.Model;
using Spire.Xls;
using System.Drawing;

namespace CertificateSystem.BLL
{
    public class ExcelExportService : IExcelExportService
    {
        private const string FixedSignerName = "郭福";
        private const string FixedCountry = "中国";
        private const string FixedSchoolCode = "11232";
        private const string FixedSchoolName = "北京信息科技大学";

        private static readonly string[] GraduationHeaders =
        {
            "KSH", "XM", "XB", "CSRQ", "ZJLX", "ZJHM", "YXDM", "YXMC", "ZYDM", "ZYMC", "XZ", "XXXS", "CC", "RXRQ", "BYRQ", "BJYJL", "ZSBH", "XZM", "BZ"
        };

        private static readonly string[] DegreeHeaders =
        {
            "XM", "XB", "CSRQ", "ZJLX", "ZJHM", "GB", "KSH", "XH", "PYDWM", "PYDW", "ZYDM", "ZYMC", "XWSYDWM", "XWSYDW", "XZXM", "ZXXM", "XWLBM", "XWLB", "HXWRQ", "XWZSBH", "DSXM", "LWLX", "LWTM", "LWGJC", "LWXTLY", "LWYJFX", "LWZXYZ"
        };

        private readonly ICertificateService _certificateService;
        private readonly IStudentPaperRepository _studentPaperRepository;

        public ExcelExportService(ICertificateService certificateService, IStudentPaperRepository studentPaperRepository)
        {
            _certificateService = certificateService;
            _studentPaperRepository = studentPaperRepository;
        }

        public async Task<byte[]> ExportGraduationExcelAsync(StudentCertificateQueryDto filter)
        {
            var certificates = await LoadCertificatesAsync(filter, "毕业证书");
            var rows = certificates.Select(BuildGraduationRow).ToList();
            return BuildWorkbook("毕业证书数据", GraduationHeaders, rows);
        }

        public async Task<byte[]> ExportCompletionExcelAsync(StudentCertificateQueryDto filter)
        {
            var certificates = await LoadCertificatesAsync(filter, "结业证书");
            var rows = certificates.Select(BuildGraduationRow).ToList();
            return BuildWorkbook("结业证书数据", GraduationHeaders, rows);
        }

        public async Task<byte[]> ExportDegreeExcelAsync(StudentCertificateQueryDto filter)
        {
            var certificates = await LoadCertificatesAsync(filter, "学位证书");
            var paperLookup = await BuildPaperLookupAsync(certificates);
            var rows = certificates.Select(x => BuildDegreeRow(x, ResolvePaper(paperLookup, x.StudentId))).ToList();
            return BuildWorkbook("学位证书数据", DegreeHeaders, rows);
        }

        public async Task<byte[]> ExportSecondDegreeExcelAsync(StudentCertificateQueryDto filter)
        {
            var certificates = await LoadCertificatesAsync(filter, "第二学位证书");
            var paperLookup = await BuildPaperLookupAsync(certificates);
            var rows = certificates.Select(x => BuildDegreeRow(x, ResolvePaper(paperLookup, x.StudentId))).ToList();
            return BuildWorkbook("第二学位证书数据", DegreeHeaders, rows);
        }

        private async Task<List<StudentCertificate>> LoadCertificatesAsync(StudentCertificateQueryDto filter, string certificateType)
        {
            filter ??= new StudentCertificateQueryDto();
            filter.CertificateType = certificateType;
            filter.PageIndex = 1;
            filter.PageSize = int.MaxValue;
            return await _certificateService.GetListByFilterAsync(filter, null);
        }

        private async Task<Dictionary<string, StudentPaper>> BuildPaperLookupAsync(IEnumerable<StudentCertificate> certificates)
        {
            var studentIds = certificates
                .Select(x => x.StudentId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var papers = await _studentPaperRepository.GetByStudentIdsAsync(studentIds);
            return papers
                .GroupBy(x => x.STU_NO ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(p => p.YEAR).ThenByDescending(p => p.Id).First(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static StudentPaper? ResolvePaper(IReadOnlyDictionary<string, StudentPaper> lookup, string? studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return null;
            }

            return lookup.TryGetValue(studentId, out var paper) ? paper : null;
        }

        private static object[] BuildGraduationRow(StudentCertificate item)
        {
            return new object[]
            {
                item.ExamNo ?? string.Empty,
                item.Name,
                item.Gender ?? string.Empty,
                FormatDate(item.BirthDate),
                item.IdCardType ?? string.Empty,
                item.IdCardNo ?? string.Empty,
                item.InstituteCode ?? string.Empty,
                item.Institute,
                item.MajorCode ?? string.Empty,
                item.Major,
                item.StudyYears?.ToString() ?? string.Empty,
                item.StudyMode ?? string.Empty,
                item.EducationLevel ?? string.Empty,
                FormatDate(item.EnrollmentDate),
                FormatDate(item.GraduationDate),
                item.GraduationConclusion ?? string.Empty,
                item.CertificateNumber ?? string.Empty,
                FixedSignerName,
                string.Empty
            };
        }

        private static object[] BuildDegreeRow(StudentCertificate item, StudentPaper? paper)
        {
            return new object[]
            {
                item.Name,
                item.Gender ?? string.Empty,
                FormatDate(item.BirthDate),
                item.IdCardType ?? string.Empty,
                item.IdCardNo ?? string.Empty,
                FixedCountry,
                item.ExamNo ?? string.Empty,
                item.StudentId,
                FixedSchoolCode,
                FixedSchoolName,
                item.MajorCode ?? string.Empty,
                item.Major,
                FixedSchoolCode,
                FixedSchoolName,
                FixedSignerName,
                FixedSignerName,
                item.AwardedDegreeCode ?? string.Empty,
                item.AwardedDegree ?? string.Empty,
                FormatDate(item.DegreeAwardDate),
                item.DegreeCertificateNumber ?? string.Empty,
                paper?.TEACHER_NAME ?? string.Empty,
                paper?.GRAD_TYPE_NAME ?? string.Empty,
                paper?.GRAD_NAME ?? string.Empty,
                paper?.KEYWORDS ?? string.Empty,
                paper?.PAPER_SOURCE_NAME ?? string.Empty,
                paper?.DIRECTION ?? string.Empty,
                paper?.LABGUAGE_NAME ?? string.Empty
            };
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyyMMdd") : string.Empty;
        }

        private static byte[] BuildWorkbook(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<object[]> rows)
        {
            using var workbook = new Workbook();
            workbook.CreateEmptySheets(1);
            var sheet = workbook.Worksheets[0];
            sheet.Name = sheetName;

            for (var i = 0; i < headers.Count; i++)
            {
                var cell = sheet.Range[1, i + 1];
                cell.Text = headers[i];
                cell.Style.Font.IsBold = true;
                cell.Style.Color = Color.LightGray;
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    sheet.Range[rowIndex + 2, columnIndex + 1].Text = row[columnIndex]?.ToString() ?? string.Empty;
                }
            }

            sheet.AllocatedRange.AutoFitColumns();

            using var stream = new MemoryStream();
            workbook.SaveToStream(stream, FileFormat.Version2013);
            return stream.ToArray();
        }
    }
}
