using System.Globalization;

namespace CertificateSystem.Model
{
    public static class StudentCertificateSyncMapping
    {
        public static StudentCertificate ToStudentCertificate(this OracleStudentRawDto raw, string syncBatchId)
        {
            return new StudentCertificate
            {
                CertificateType = raw.CertificateType,
                GraduationYear = raw.BYJMC ?? string.Empty,
                Grade= raw.XZNJ ?? string.Empty,
                GraduationYearName = raw.BYJMC,
                InstituteCode = raw.YXDM ?? string.Empty,
                Institute = raw.YXMC ?? string.Empty,
                MajorCode = raw.ZYDM ?? string.Empty,
                Major = raw.ZYMC ?? string.Empty,
                ClassName = raw.BJMC ?? string.Empty,
                StudentId = raw.XH ?? string.Empty,
                Name = raw.XM ?? string.Empty,
                Gender = raw.XB,
                Nation = raw.MZ,
                PoliticalStatus = raw.ZZMM,
                IdCardType = raw.SFZJLX,
                IdCardNo = raw.SFZJH,
                ExamNo = raw.KSH,
                StudyMode = raw.XXXS,
                BirthDate = ParseNullableDate(raw.CSRQ),
                EnrollmentDate = ParseNullableDate(raw.RXNY),
                GraduationDate = ParseNullableDate(raw.SJBYRQ),
                GraduationConclusion = raw.BJYJL,
                StudyYears = ParseNullableInt(raw.XZ),
                EducationLevel = raw.PYCC,
                CertificateNumber = ResolveCertificateNumber(raw),
                CertificateDate = ResolveCertificateDate(raw),
                PhotoPath = null,
                IsDegreeAwarded = raw.SFSYXW,
                AwardedDegree = raw.SYXW,
                DegreeCertificateNumber = raw.XWZH,
                DegreeAwardDate = ParseNullableDate(raw.XWSYSJ),
                Gpa = raw.BISTUGPA,
                IsRegistered = raw.SFZC,
                AwardedDegreeCode = raw.SYXWDM,
                ZSZPB = raw.ZSZPB,
                XWZPB = raw.XWZPB,
                XJZPB = raw.XJZPB,
                BYZPB = raw.BYZPB,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SyncBatchId = syncBatchId
            };
        }

        private static string? ResolveCertificateNumber(OracleStudentRawDto raw)
        {
            return raw.CertificateType switch
            {
                "毕业证书" => raw.BYZSH,
                "学位证书" => raw.XWZH,
                "结业证书" => raw.JYZSH,
                "第二学位证书" => raw.XWZH,
                _ => raw.BYZSH ?? raw.JYZSH ?? raw.XWZH
            };
        }

        private static DateTime? ResolveCertificateDate(OracleStudentRawDto raw)
        {
            return raw.CertificateType switch
            {
                "学位证书" => ParseNullableDate(raw.XWSYSJ),
                _ => ParseNullableDate(raw.SJBYRQ)
            };
        }

        private static DateTime? ParseNullableDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var formats = new[] { "yyyy-MM-dd", "yyyy/M/d", "yyyy-MM", "yyyy/M", "yyyyMMdd", "yyyy.MM.dd" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            if (DateTime.TryParse(value, out result))
                return result;

            return null;
        }

        private static int? ParseNullableInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value.Trim(), out var result) ? result : null;
        }
    }
}
