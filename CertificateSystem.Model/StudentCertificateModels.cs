namespace CertificateSystem.Model
{
    public class StudentCertificate
    {
        public long Id { get; set; }
        public string CertificateType { get; set; } = string.Empty;
        public string GraduationYear { get; set; } = string.Empty;
        public string? GraduationYearName { get; set; }
        public string Institute { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? Nation { get; set; }
        public string? PoliticalStatus { get; set; }
        public string? IdCardType { get; set; }
        public string? IdCardNo { get; set; }
        public string? ExamNo { get; set; }
        public string? StudyMode { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public DateTime? GraduationDate { get; set; }
        public string? GraduationConclusion { get; set; }
        public int? StudyYears { get; set; }
        public string? EducationLevel { get; set; }
        public string? CertificateNumber { get; set; }
        public DateTime? CertificateDate { get; set; }
        public string? PhotoPath { get; set; }
        public string? IsDegreeAwarded { get; set; }
        public string? AwardedDegree { get; set; }
        public string? DegreeCertificateNumber { get; set; }
        public DateTime? DegreeAwardDate { get; set; }
        public string? Gpa { get; set; }
        public string? IsRegistered { get; set; }
        public string? AwardedDegreeCode { get; set; }
        public byte[]? ZSZPB { get; set; }
        public byte[]? XWZPB { get; set; }
        public byte[]? XJZPB { get; set; }
        public byte[]? BYZPB { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? SyncBatchId { get; set; }
    }

    public class StudentCertificateQueryDto
    {
        public string? CertificateType { get; set; }
        public string? GraduationYear { get; set; }
        public string? Institute { get; set; }
        public string? Major { get; set; }
        public string? ClassName { get; set; }
        public string? StudentIdOrName { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PageResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class BatchPrintRequestDto
    {
        public string CertificateType { get; set; } = string.Empty;
        public StudentCertificateQueryDto Filter { get; set; } = new();
    }

    public class BatchPrintTaskResultDto
    {
        public string TaskId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
