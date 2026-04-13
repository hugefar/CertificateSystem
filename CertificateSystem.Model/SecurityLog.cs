namespace CertificateSystem.Model
{
    public class SecurityLog
    {
        public long Id { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string OperationModule { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? OperatorUserId { get; set; }
        public string? OperatorName { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SecurityLogQueryDto
    {
        public string? OperationType { get; set; }
        public string? OperationModule { get; set; }
        public string? OperatorName { get; set; }
        public string? Keyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}