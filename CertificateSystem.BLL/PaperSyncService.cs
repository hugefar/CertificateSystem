using System.Data;
using CertificateSystem.DAL;
using CertificateSystem.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CertificateSystem.BLL
{
    public class PaperSyncService : IPaperSyncService
    {
        private readonly IOraclePaperRepository _oraclePaperRepository;
        private readonly ILogService _logService;
        private readonly string _sqlConnectionString;

        public PaperSyncService(IOraclePaperRepository oraclePaperRepository, ILogService logService, IConfiguration configuration)
        {
            _oraclePaperRepository = oraclePaperRepository;
            _logService = logService;
            _sqlConnectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found.");
        }

        public async Task<SyncResult> SyncPapersAsync(CancellationToken cancellationToken = default)
        {
            var syncBatchId = Guid.NewGuid().ToString("N");
            await _logService.LogAsync("同步开始", "学生论文同步", $"开始同步学生论文数据，批次号 {syncBatchId}", string.Empty, "System", "127.0.0.1");

            try
            {
                var raws = await _oraclePaperRepository.GetAllPapersAsync(cancellationToken);
                var papers = raws.Select(x => MapToEntity(x, syncBatchId)).ToList();
                await ReplaceAllAsync(papers, cancellationToken);

                await _logService.LogAsync("同步完成", "学生论文同步", $"学生论文同步完成，批次号 {syncBatchId}，共 {papers.Count} 条。", string.Empty, "System", "127.0.0.1");

                return new SyncResult
                {
                    Success = true,
                    TotalRecords = papers.Count,
                    InsertedCount = papers.Count,
                    Message = $"学生论文同步完成，共 {papers.Count} 条。",
                    ExecutedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("同步失败", "学生论文同步", $"学生论文同步失败，原因：{ex.Message}", string.Empty, "System", "127.0.0.1");
                return new SyncResult
                {
                    Success = false,
                    Message = "学生论文同步失败：" + ex.Message,
                    Errors = new List<string> { ex.Message },
                    ExecutedAt = DateTime.Now
                };
            }
        }

        private static StudentPaper MapToEntity(OraclePaperRawDto raw, string syncBatchId)
        {
            return new StudentPaper
            {
                PAPER_ID = Normalize(raw.ID),
                YEAR = Normalize(raw.YEAR),
                STU_NO = Normalize(raw.STU_NO),
                STU_NAME = Normalize(raw.STU_NAME),
                TEACHER_NO = Normalize(raw.TEACHER_NO),
                TEACHER_NAME = Normalize(raw.TEACHER_NAME),
                GRADE = Normalize(raw.GRADE),
                CLASS_CODE = Normalize(raw.CLASS_CODE),
                CLASS_NAME = Normalize(raw.CLASS_NAME),
                SPEC_CODE = Normalize(raw.SPEC_CODE),
                SPEC_NAME = Normalize(raw.SPEC_NAME),
                DEP_NAME = Normalize(raw.DEP_NAME),
                GRAD_TYPE_NAME = Normalize(raw.GRAD_TYPE_NAME),
                GRAD_NAME = Normalize(raw.GRAD_NAME),
                KEYWORDS = Normalize(raw.KEYWORDS),
                PAPER_SOURCE_NAME = Normalize(raw.PAPER_SOURCE_NAME),
                DIRECTION = Normalize(raw.DIRECTION),
                LABGUAGE_NAME = Normalize(raw.LABGUAGE_NAME),
                FINAL_SCORE = raw.FINAL_SCORE,
                TEA_NAMES = Normalize(raw.TEA_NAMES),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SyncBatchId = syncBatchId
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task ReplaceAllAsync(List<StudentPaper> papers, CancellationToken cancellationToken)
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(cancellationToken);
            await using var tran = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                await using (var deleteCmd = new SqlCommand("TRUNCATE TABLE dbo.StudentPapers", conn, tran))
                {
                    await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                using var table = BuildDataTable(papers);
                using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran)
                {
                    DestinationTableName = "dbo.StudentPapers",
                    BatchSize = 500,
                    BulkCopyTimeout = 0
                };

                foreach (DataColumn column in table.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(table, cancellationToken);
                await tran.CommitAsync(cancellationToken);
            }
            catch
            {
                await tran.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static DataTable BuildDataTable(IEnumerable<StudentPaper> papers)
        {
            var table = new DataTable();
            table.Columns.Add("PAPER_ID", typeof(string));
            table.Columns.Add("YEAR", typeof(string));
            table.Columns.Add("STU_NO", typeof(string));
            table.Columns.Add("STU_NAME", typeof(string));
            table.Columns.Add("TEACHER_NO", typeof(string));
            table.Columns.Add("TEACHER_NAME", typeof(string));
            table.Columns.Add("GRADE", typeof(string));
            table.Columns.Add("CLASS_CODE", typeof(string));
            table.Columns.Add("CLASS_NAME", typeof(string));
            table.Columns.Add("SPEC_CODE", typeof(string));
            table.Columns.Add("SPEC_NAME", typeof(string));
            table.Columns.Add("DEP_NAME", typeof(string));
            table.Columns.Add("GRAD_TYPE_NAME", typeof(string));
            table.Columns.Add("GRAD_NAME", typeof(string));
            table.Columns.Add("KEYWORDS", typeof(string));
            table.Columns.Add("PAPER_SOURCE_NAME", typeof(string));
            table.Columns.Add("DIRECTION", typeof(string));
            table.Columns.Add("LABGUAGE_NAME", typeof(string));
            table.Columns.Add("FINAL_SCORE", typeof(decimal));
            table.Columns.Add("TEA_NAMES", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("SyncBatchId", typeof(string));

            foreach (var item in papers)
            {
                table.Rows.Add(
                    (object?)item.PAPER_ID ?? DBNull.Value,
                    (object?)item.YEAR ?? DBNull.Value,
                    (object?)item.STU_NO ?? DBNull.Value,
                    (object?)item.STU_NAME ?? DBNull.Value,
                    (object?)item.TEACHER_NO ?? DBNull.Value,
                    (object?)item.TEACHER_NAME ?? DBNull.Value,
                    (object?)item.GRADE ?? DBNull.Value,
                    (object?)item.CLASS_CODE ?? DBNull.Value,
                    (object?)item.CLASS_NAME ?? DBNull.Value,
                    (object?)item.SPEC_CODE ?? DBNull.Value,
                    (object?)item.SPEC_NAME ?? DBNull.Value,
                    (object?)item.DEP_NAME ?? DBNull.Value,
                    (object?)item.GRAD_TYPE_NAME ?? DBNull.Value,
                    (object?)item.GRAD_NAME ?? DBNull.Value,
                    (object?)item.KEYWORDS ?? DBNull.Value,
                    (object?)item.PAPER_SOURCE_NAME ?? DBNull.Value,
                    (object?)item.DIRECTION ?? DBNull.Value,
                    (object?)item.LABGUAGE_NAME ?? DBNull.Value,
                    (object?)item.FINAL_SCORE ?? DBNull.Value,
                    (object?)item.TEA_NAMES ?? DBNull.Value,
                    item.CreatedAt == default ? DateTime.Now : item.CreatedAt,
                    (object?)item.UpdatedAt ?? DateTime.Now,
                    (object?)item.SyncBatchId ?? DBNull.Value);
            }

            return table;
        }
    }
}
