using System.Text;
using CertificateSystem.DBUtility;
using CertificateSystem.Model;
using Microsoft.Data.SqlClient;

namespace CertificateSystem.DAL
{
    public interface IStudentPaperRepository
    {
        Task<List<StudentPaper>> GetByStudentIdsAsync(IEnumerable<string> studentIds);
    }

    public class StudentPaperRepository : IStudentPaperRepository
    {
        public async Task<List<StudentPaper>> GetByStudentIdsAsync(IEnumerable<string> studentIds)
        {
            var ids = studentIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (ids.Count == 0)
            {
                return new List<StudentPaper>();
            }

            const int batchSize = 2000;
            var list = new List<StudentPaper>();

            await using var conn = SqlHelper.CreateConnection();
            await conn.OpenAsync();

            for (var offset = 0; offset < ids.Count; offset += batchSize)
            {
                var batch = ids.Skip(offset).Take(batchSize).ToList();
                var sql = new StringBuilder();
                sql.AppendLine("SELECT Id, PAPER_ID, YEAR, STU_NO, STU_NAME, TEACHER_NO, TEACHER_NAME, GRADE, CLASS_CODE, CLASS_NAME,");
                sql.AppendLine("       SPEC_CODE, SPEC_NAME, DEP_NAME, GRAD_TYPE_NAME, GRAD_NAME, KEYWORDS, PAPER_SOURCE_NAME, DIRECTION,");
                sql.AppendLine("       LABGUAGE_NAME, FINAL_SCORE, TEA_NAMES, CreatedAt, UpdatedAt, SyncBatchId");
                sql.AppendLine("FROM dbo.StudentPapers");
                sql.Append("WHERE STU_NO IN (");

                var parameters = new List<SqlParameter>();
                for (var i = 0; i < batch.Count; i++)
                {
                    if (i > 0)
                    {
                        sql.Append(", ");
                    }

                    var parameterName = "@StudentId" + i;
                    sql.Append(parameterName);
                    parameters.Add(new SqlParameter(parameterName, batch[i]));
                }

                sql.AppendLine(")");
                sql.AppendLine("ORDER BY YEAR DESC, STU_NO ASC, Id DESC");

                await using var cmd = new SqlCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(Map(reader));
                }
            }

            return list;
        }

        private static StudentPaper Map(SqlDataReader reader)
        {
            return new StudentPaper
            {
                Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                PAPER_ID = reader["PAPER_ID"] == DBNull.Value ? null : reader["PAPER_ID"].ToString(),
                YEAR = reader["YEAR"] == DBNull.Value ? null : reader["YEAR"].ToString(),
                STU_NO = reader["STU_NO"] == DBNull.Value ? null : reader["STU_NO"].ToString(),
                STU_NAME = reader["STU_NAME"] == DBNull.Value ? null : reader["STU_NAME"].ToString(),
                TEACHER_NO = reader["TEACHER_NO"] == DBNull.Value ? null : reader["TEACHER_NO"].ToString(),
                TEACHER_NAME = reader["TEACHER_NAME"] == DBNull.Value ? null : reader["TEACHER_NAME"].ToString(),
                GRADE = reader["GRADE"] == DBNull.Value ? null : reader["GRADE"].ToString(),
                CLASS_CODE = reader["CLASS_CODE"] == DBNull.Value ? null : reader["CLASS_CODE"].ToString(),
                CLASS_NAME = reader["CLASS_NAME"] == DBNull.Value ? null : reader["CLASS_NAME"].ToString(),
                SPEC_CODE = reader["SPEC_CODE"] == DBNull.Value ? null : reader["SPEC_CODE"].ToString(),
                SPEC_NAME = reader["SPEC_NAME"] == DBNull.Value ? null : reader["SPEC_NAME"].ToString(),
                DEP_NAME = reader["DEP_NAME"] == DBNull.Value ? null : reader["DEP_NAME"].ToString(),
                GRAD_TYPE_NAME = reader["GRAD_TYPE_NAME"] == DBNull.Value ? null : reader["GRAD_TYPE_NAME"].ToString(),
                GRAD_NAME = reader["GRAD_NAME"] == DBNull.Value ? null : reader["GRAD_NAME"].ToString(),
                KEYWORDS = reader["KEYWORDS"] == DBNull.Value ? null : reader["KEYWORDS"].ToString(),
                PAPER_SOURCE_NAME = reader["PAPER_SOURCE_NAME"] == DBNull.Value ? null : reader["PAPER_SOURCE_NAME"].ToString(),
                DIRECTION = reader["DIRECTION"] == DBNull.Value ? null : reader["DIRECTION"].ToString(),
                LABGUAGE_NAME = reader["LABGUAGE_NAME"] == DBNull.Value ? null : reader["LABGUAGE_NAME"].ToString(),
                FINAL_SCORE = reader["FINAL_SCORE"] == DBNull.Value ? null : Convert.ToDecimal(reader["FINAL_SCORE"]),
                TEA_NAMES = reader["TEA_NAMES"] == DBNull.Value ? null : reader["TEA_NAMES"].ToString(),
                CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"]),
                SyncBatchId = reader["SyncBatchId"] == DBNull.Value ? null : reader["SyncBatchId"].ToString()
            };
        }
    }
}
