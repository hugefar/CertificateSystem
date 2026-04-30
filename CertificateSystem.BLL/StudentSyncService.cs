using System.Data;
using Microsoft.Data.SqlClient;
using CertificateSystem.DAL;
using CertificateSystem.Model;
using Microsoft.Extensions.Configuration;

namespace CertificateSystem.BLL
{
    public interface IStudentSyncService
    {
        Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
    }

    public class StudentSyncService : IStudentSyncService
    {
        private readonly IOracleStudentRepository _oracleStudentRepository;
        private readonly ILogService _logService;
        private readonly string _sqlConnectionString;

        public StudentSyncService(IOracleStudentRepository oracleStudentRepository, ILogService logService, IConfiguration configuration)
        {
            _oracleStudentRepository = oracleStudentRepository;
            _logService = logService;
            _sqlConnectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found.");
        }

        public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        {
            var syncBatchId = Guid.NewGuid().ToString("N");
            await _logService.LogAsync("同步开始", "学生证书同步", $"开始同步学生证书数据，批次号 {syncBatchId}", string.Empty, "System", "127.0.0.1");

            try
            {
                var graduationAndDegree = await _oracleStudentRepository.GetGraduationAndDegreeStudentsAsync(cancellationToken);
                var completion = await _oracleStudentRepository.GetCompletionStudentsAsync(cancellationToken);
                var secondDegree = await _oracleStudentRepository.GetSecondDegreeStudentsAsync(cancellationToken);

                var mapped = new List<StudentCertificate>();
                mapped.AddRange(MapGraduationAndDegree(graduationAndDegree, syncBatchId));
                mapped.AddRange(MapCompletion(completion, syncBatchId));
                mapped.AddRange(MapSecondDegree(secondDegree, syncBatchId));

                await ReplaceAllAsync(mapped, syncBatchId, cancellationToken);

                await _logService.LogAsync("同步完成", "学生证书同步", $"学生证书同步完成，批次号 {syncBatchId}，共 {mapped.Count} 条。", string.Empty, "System", "127.0.0.1");
                return new SyncResult
                {
                    Success = true,
                    TotalRecords = mapped.Count,
                    InsertedCount = mapped.Count,
                    Message = $"学生证书同步完成，共 {mapped.Count} 条。",
                    ExecutedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("同步失败", "学生证书同步", $"学生证书同步失败，原因：{ex.Message}", string.Empty, "System", "127.0.0.1");
                return new SyncResult
                {
                    Success = false,
                    Message = "学生证书同步失败：" + ex.Message,
                    Errors = new List<string> { ex.Message },
                    ExecutedAt = DateTime.Now
                };
            }
        }

        private static IEnumerable<StudentCertificate> MapGraduationAndDegree(IEnumerable<OracleStudentRawDto> raws, string syncBatchId)
        {
            foreach (var raw in raws)
            {
                var graduation = raw;
                graduation.CertificateType = "毕业证书";
                yield return graduation.ToStudentCertificate(syncBatchId);

                if (!string.IsNullOrWhiteSpace(raw.XWZH) || string.Equals(raw.SFSYXW, "1", StringComparison.OrdinalIgnoreCase))
                {
                    var degree = Clone(raw, "学位证书");
                    yield return degree.ToStudentCertificate(syncBatchId);
                }
            }
        }

        private static IEnumerable<StudentCertificate> MapCompletion(IEnumerable<OracleStudentRawDto> raws, string syncBatchId)
        {
            foreach (var raw in raws)
            {
                var completion = Clone(raw, "结业证书");
                yield return completion.ToStudentCertificate(syncBatchId);
            }
        }

        private static IEnumerable<StudentCertificate> MapSecondDegree(IEnumerable<OracleStudentRawDto> raws, string syncBatchId)
        {
            foreach (var raw in raws)
            {
                var second = Clone(raw, "第二学位证书");
                yield return second.ToStudentCertificate(syncBatchId);
            }
        }

        private static OracleStudentRawDto Clone(OracleStudentRawDto raw, string certificateType)
        {
            return new OracleStudentRawDto
            {
                ViewSource = raw.ViewSource,
                CertificateType = certificateType,
                XH = raw.XH,
                XM = raw.XM,
                XZNJ = raw.XZNJ,
                YXMC = raw.YXMC,
                YXDM= raw.YXDM,
                ZYDM = raw.ZYDM,
                ZYMC = raw.ZYMC,
                BJMC = raw.BJMC,
                XB = raw.XB,
                MZ = raw.MZ,
                ZZMM = raw.ZZMM,
                SFZJLX = raw.SFZJLX,
                SFZJH = raw.SFZJH,
                KSH = raw.KSH,
                XXXS = raw.XXXS,
                CSRQ = raw.CSRQ,
                BJYJL = raw.BJYJL,
                BYZSH = raw.BYZSH,
                JYZSH = raw.JYZSH,
                SJBYRQ = raw.SJBYRQ,
                SFSYXW = raw.SFSYXW,
                SYXW = raw.SYXW,
                XWZH = raw.XWZH,
                XWSYSJ = raw.XWSYSJ,
                ZSZPB = raw.ZSZPB,
                XWZPB = raw.XWZPB,
                XJZPB = raw.XJZPB,
                BYZPB = raw.BYZPB,
                XZ = raw.XZ,
                PYCC = raw.PYCC,
                RXNY = raw.RXNY,
                SFZC = raw.SFZC,
                BISTUGPA = raw.BISTUGPA,
                BYJMC = raw.BYJMC,
                SYXWDM = raw.SYXWDM
            };
        }

        private async Task ReplaceAllAsync(List<StudentCertificate> data, string syncBatchId, CancellationToken cancellationToken)
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(cancellationToken);
            await using var tran = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                await using (var deleteCmd = new SqlCommand("truncate table dbo.StudentCertificates", conn, tran))
                {
                    await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                using var table = BuildDataTable(data, syncBatchId);
                using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran)
                {
                    DestinationTableName = "dbo.StudentCertificates",
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

        private static DataTable BuildDataTable(IEnumerable<StudentCertificate> data, string syncBatchId)
        {
            var table = new DataTable();
            table.Columns.Add("CertificateType", typeof(string));
            table.Columns.Add("GraduationYear", typeof(string));
            table.Columns.Add("InstituteCode", typeof(string));
            table.Columns.Add("Institute", typeof(string));
            table.Columns.Add("MajorCode", typeof(string));
            table.Columns.Add("Major", typeof(string));
            table.Columns.Add("ClassName", typeof(string));
            table.Columns.Add("StudentId", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Gender", typeof(string));
            table.Columns.Add("BirthDate", typeof(DateTime));
            table.Columns.Add("EnrollmentDate", typeof(DateTime));
            table.Columns.Add("GraduationDate", typeof(DateTime));
            table.Columns.Add("StudyYears", typeof(int));
            table.Columns.Add("EducationLevel", typeof(string));
            table.Columns.Add("CertificateNumber", typeof(string));
            table.Columns.Add("CertificateDate", typeof(DateTime));
            table.Columns.Add("PhotoPath", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("SyncBatchId", typeof(string));
            table.Columns.Add("GraduationYearName", typeof(string));
            table.Columns.Add("Nation", typeof(string));
            table.Columns.Add("PoliticalStatus", typeof(string));
            table.Columns.Add("IdCardType", typeof(string));
            table.Columns.Add("IdCardNo", typeof(string));
            table.Columns.Add("ExamNo", typeof(string));
            table.Columns.Add("StudyMode", typeof(string));
            table.Columns.Add("GraduationConclusion", typeof(string));
            table.Columns.Add("IsDegreeAwarded", typeof(string));
            table.Columns.Add("AwardedDegree", typeof(string));
            table.Columns.Add("DegreeCertificateNumber", typeof(string));
            table.Columns.Add("DegreeAwardDate", typeof(DateTime));
            table.Columns.Add("Gpa", typeof(string));
            table.Columns.Add("IsRegistered", typeof(string));
            table.Columns.Add("AwardedDegreeCode", typeof(string));
            table.Columns.Add("ZSZPB", typeof(byte[]));
            table.Columns.Add("XWZPB", typeof(byte[]));
            table.Columns.Add("XJZPB", typeof(byte[]));
            table.Columns.Add("BYZPB", typeof(byte[]));
            table.Columns.Add("Grade", typeof(string));

            foreach (var item in data)
            {
                table.Rows.Add(
                    item.CertificateType,
                    item.GraduationYear,
                    item.InstituteCode,
                    item.Institute,
                    item.MajorCode,
                    item.Major,
                    item.ClassName,
                    item.StudentId,
                    item.Name,
                    (object?)item.Gender ?? DBNull.Value,
                    (object?)item.BirthDate ?? DBNull.Value,
                    (object?)item.EnrollmentDate ?? DBNull.Value,
                    (object?)item.GraduationDate ?? DBNull.Value,
                    (object?)item.StudyYears ?? DBNull.Value,
                    (object?)item.EducationLevel ?? DBNull.Value,
                    (object?)item.CertificateNumber ?? DBNull.Value,
                    (object?)item.CertificateDate ?? DBNull.Value,
                    (object?)item.PhotoPath ?? DBNull.Value,
                    item.CreatedAt == default ? DateTime.Now : item.CreatedAt,
                    (object?)item.UpdatedAt ?? DateTime.Now,
                    item.SyncBatchId ?? syncBatchId,
                    (object?)item.GraduationYearName ?? DBNull.Value,
                    (object?)item.Nation ?? DBNull.Value,
                    (object?)item.PoliticalStatus ?? DBNull.Value,
                    (object?)item.IdCardType ?? DBNull.Value,
                    (object?)item.IdCardNo ?? DBNull.Value,
                    (object?)item.ExamNo ?? DBNull.Value,
                    (object?)item.StudyMode ?? DBNull.Value,
                    (object?)item.GraduationConclusion ?? DBNull.Value,
                    (object?)item.IsDegreeAwarded ?? DBNull.Value,
                    (object?)item.AwardedDegree ?? DBNull.Value,
                    (object?)item.DegreeCertificateNumber ?? DBNull.Value,
                    (object?)item.DegreeAwardDate ?? DBNull.Value,
                    (object?)item.Gpa ?? DBNull.Value,
                    (object?)item.IsRegistered ?? DBNull.Value,
                    (object?)item.AwardedDegreeCode ?? DBNull.Value,
                    (object?)item.ZSZPB ?? DBNull.Value,
                    (object?)item.XWZPB ?? DBNull.Value,
                    (object?)item.XJZPB ?? DBNull.Value,
                    (object?)item.BYZPB ?? DBNull.Value,
                    (object?)item.Grade ?? DBNull.Value);
            }

            return table;
        }
    }
}
