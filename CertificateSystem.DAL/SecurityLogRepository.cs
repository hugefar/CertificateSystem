using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using CertificateSystem.DBUtility;
using CertificateSystem.Model;

namespace CertificateSystem.DAL
{
    public interface ISecurityLogRepository
    {
        Task<int> InsertAsync(SecurityLog log);
        Task<PagedResult<SecurityLog>> GetPagedListAsync(SecurityLogQueryDto query);
    }

    public class SecurityLogRepository : ISecurityLogRepository
    {
        public async Task<int> InsertAsync(SecurityLog log)
        {
            const string sql = @"
INSERT INTO dbo.SecurityLogs
(OperationType, OperationModule, Content, OperatorUserId, OperatorName, IPAddress, CreatedAt)
VALUES
(@OperationType, @OperationModule, @Content, @OperatorUserId, @OperatorName, @IPAddress, @CreatedAt)";

            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@OperationType", log.OperationType));
            cmd.Parameters.Add(new SqlParameter("@OperationModule", log.OperationModule));
            cmd.Parameters.Add(new SqlParameter("@Content", log.Content));
            cmd.Parameters.Add(new SqlParameter("@OperatorUserId", (object?)log.OperatorUserId ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@OperatorName", (object?)log.OperatorName ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IPAddress", (object?)log.IPAddress ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CreatedAt", log.CreatedAt == default ? DateTime.Now : log.CreatedAt));
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<PagedResult<SecurityLog>> GetPagedListAsync(SecurityLogQueryDto query)
        {
            query ??= new SecurityLogQueryDto();
            if (query.PageIndex <= 0) query.PageIndex = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var parameters = new List<SqlParameter>();
            var whereSql = BuildWhereSql(query, parameters);

            var countSql = $"SELECT COUNT(1) FROM dbo.SecurityLogs {whereSql}";
            var pageSql = $@"
SELECT Id, OperationType, OperationModule, Content, OperatorUserId, OperatorName, IPAddress, CreatedAt
FROM dbo.SecurityLogs
{whereSql}
ORDER BY CreatedAt DESC, Id DESC
OFFSET (@PageIndex - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add(new SqlParameter("@PageIndex", query.PageIndex));
            parameters.Add(new SqlParameter("@PageSize", query.PageSize));

            await using var conn = SqlHelper.CreateConnection();
            await conn.OpenAsync();

            int totalCount;
            await using (var countCmd = new SqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddRange(CloneParameters(parameters.Where(x => x.ParameterName != "@PageIndex" && x.ParameterName != "@PageSize").ToList()));
                var scalar = await countCmd.ExecuteScalarAsync();
                totalCount = scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
            }

            var items = new List<SecurityLog>();
            await using (var cmd = new SqlCommand(pageSql, conn))
            {
                cmd.Parameters.AddRange(CloneParameters(parameters));
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(Map(reader));
                }
            }

            return new PagedResult<SecurityLog>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        private static string BuildWhereSql(SecurityLogQueryDto query, List<SqlParameter> parameters)
        {
            var sb = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(query.OperationType))
            {
                sb.Append(" AND OperationType = @OperationType");
                parameters.Add(new SqlParameter("@OperationType", query.OperationType));
            }

            if (!string.IsNullOrWhiteSpace(query.OperationModule))
            {
                sb.Append(" AND OperationModule = @OperationModule");
                parameters.Add(new SqlParameter("@OperationModule", query.OperationModule));
            }

            if (!string.IsNullOrWhiteSpace(query.OperatorName))
            {
                sb.Append(" AND OperatorName LIKE '%' + @OperatorName + '%'");
                parameters.Add(new SqlParameter("@OperatorName", query.OperatorName));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sb.Append(" AND Content LIKE '%' + @Keyword + '%'");
                parameters.Add(new SqlParameter("@Keyword", query.Keyword));
            }

            if (query.StartDate.HasValue)
            {
                sb.Append(" AND CreatedAt >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", query.StartDate.Value));
            }

            if (query.EndDate.HasValue)
            {
                sb.Append(" AND CreatedAt < @EndDateExclusive");
                parameters.Add(new SqlParameter("@EndDateExclusive", query.EndDate.Value.Date.AddDays(1)));
            }

            return sb.ToString();
        }

        private static SecurityLog Map(SqlDataReader reader)
        {
            return new SecurityLog
            {
                Id = Convert.ToInt64(reader["Id"]),
                OperationType = reader["OperationType"]?.ToString() ?? string.Empty,
                OperationModule = reader["OperationModule"]?.ToString() ?? string.Empty,
                Content = reader["Content"]?.ToString() ?? string.Empty,
                OperatorUserId = reader["OperatorUserId"] == DBNull.Value ? null : reader["OperatorUserId"].ToString(),
                OperatorName = reader["OperatorName"] == DBNull.Value ? null : reader["OperatorName"].ToString(),
                IPAddress = reader["IPAddress"] == DBNull.Value ? null : reader["IPAddress"].ToString(),
                CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        private static SqlParameter[] CloneParameters(List<SqlParameter> source)
        {
            return source.Select(p => new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value)).ToArray();
        }
    }
}
