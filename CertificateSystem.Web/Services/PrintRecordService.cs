using System.Data.SqlClient;
using CertificateSystem.DBUtility;
using CertificateSystem.Model;

namespace CertificateSystem.Web.Services
{
    public class PrintRecordService : IPrintRecordService
    {
        public Task SaveActualPrintRecordAsync(StudentCertificate certificate, string certificateType, string operatorUserId, string? remark = null, CancellationToken cancellationToken = default)
        {
            return SaveActualPrintRecordsAsync(new[] { certificate }, certificateType, operatorUserId, remark, cancellationToken);
        }

        public async Task SaveActualPrintRecordsAsync(IEnumerable<StudentCertificate> certificates, string certificateType, string operatorUserId, string? remark = null, CancellationToken cancellationToken = default)
        {
            var items = (certificates ?? Enumerable.Empty<StudentCertificate>()).Where(x => x != null).ToList();
            if (items.Count == 0)
                return;

            var now = DateTime.Now;

            await using var conn = SqlHelper.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            await using var tran = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                const string sql = @"INSERT INTO dbo.PrintRecords (PrintTime, Department, CertificateType, StudentName, StudentNo, OperatorName, Remark, CreatedAt) VALUES (@PrintTime, @Department, @CertificateType, @StudentName, @StudentNo, @OperatorName, @Remark, @CreatedAt)";

                foreach (var item in items)
                {
                    await using var cmd = new SqlCommand(sql, conn, tran);
                    cmd.Parameters.AddWithValue("@PrintTime", now);
                    cmd.Parameters.AddWithValue("@Department", (object?)item.Institute ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CertificateType", certificateType);
                    cmd.Parameters.AddWithValue("@StudentName", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@StudentNo", item.StudentId ?? string.Empty);
                    cmd.Parameters.AddWithValue("@OperatorName", operatorUserId ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", now);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await tran.CommitAsync(cancellationToken);
            }
            catch
            {
                await tran.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}