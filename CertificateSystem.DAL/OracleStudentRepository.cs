using CertificateSystem.Model;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CertificateSystem.DAL
{
    public interface IOracleStudentRepository
    {
        Task<List<OracleStudentRawDto>> GetGraduationAndDegreeStudentsAsync(CancellationToken cancellationToken = default);
        Task<List<OracleStudentRawDto>> GetCompletionStudentsAsync(CancellationToken cancellationToken = default);
        Task<List<OracleStudentRawDto>> GetSecondDegreeStudentsAsync(CancellationToken cancellationToken = default);
    }

    public class OracleStudentRepository : IOracleStudentRepository
    {
        private readonly string _connectionString;

        public OracleStudentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task<List<OracleStudentRawDto>> GetGraduationAndDegreeStudentsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT XH, XM, XZNJ, YXMC, ZYMC, BJMC, XB, MZ, ZZMM, SFZJLX, SFZJH, KSH, XXFS, CSRQ, BJYJL,
       BYZSH, JYZSH, SJBYSJ, SFSYXW, SYXW, XWZH, XWSYSJ, ZSZPB, XWZPB, XJZPB, BYZPB, XZ, PYCC, RXNY, SFZC, BISTUGPA, BYJMC, SYXWDM
FROM v_xs_byzs_FEXW";
            return ReadAsync(sql, "v_xs_byzs_FEXW", cancellationToken);
        }

        public Task<List<OracleStudentRawDto>> GetCompletionStudentsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT XH, XM, XZNJ, YXMC, ZYMC, BJMC, XB, MZ, ZZMM, SFZJLX, SFZJH, KSH, XXFS, CSRQ, BJYJL,
       BYZSH, JYZSH, SJBYSJ, SFSYXW, SYXW, XWZH, XWSYSJ, ZSZPB, XWZPB, XJZPB, BYZPB, XZ, PYCC, RXNY, SFZC, BISTUGPA, BYJMC, SYXWDM
FROM v_xs_JYzs";
            return ReadAsync(sql, "v_xs_JYzs", cancellationToken);
        }

        public Task<List<OracleStudentRawDto>> GetSecondDegreeStudentsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT XH, XM, XZNJ, YXMC, ZYMC, BJMC, XB, MZ, ZZMM, SFZJLX, SFZJH, KSH, XXFS, CSRQ, BJYJL,
       BYZSH, JYZSH, SJBYSJ, SFSYXW, SYXW, XWZH, XWSYSJ, ZSZPB, XWZPB, XJZPB, BYZPB, XZ, PYCC, RXNY, SFZC, BISTUGPA, BYJMC, SYXWDM
FROM v_xs_EXWzs";
            return ReadAsync(sql, "v_xs_EXWzs", cancellationToken);
        }

        private async Task<List<OracleStudentRawDto>> ReadAsync(string sql, string source, CancellationToken cancellationToken)
        {
            var list = new List<OracleStudentRawDto>();
            await using var conn = new OracleConnection(_connectionString);
            await using var cmd = new OracleCommand(sql, conn)
            {
                BindByName = true
            };
            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new OracleStudentRawDto
                {
                    ViewSource = source,
                    XH = GetString(reader, "XH"),
                    XM = GetString(reader, "XM"),
                    XZNJ = GetString(reader, "XZNJ"),
                    YXMC = GetString(reader, "YXMC"),
                    ZYMC = GetString(reader, "ZYMC"),
                    BJMC = GetString(reader, "BJMC"),
                    XB = GetString(reader, "XB"),
                    MZ = GetString(reader, "MZ"),
                    ZZMM = GetString(reader, "ZZMM"),
                    SFZJLX = GetString(reader, "SFZJLX"),
                    SFZJH = GetString(reader, "SFZJH"),
                    KSH = GetString(reader, "KSH"),
                    XXFS = GetString(reader, "XXFS"),
                    CSRQ = GetString(reader, "CSRQ"),
                    BJYJL = GetString(reader, "BJYJL"),
                    BYZSH = GetString(reader, "BYZSH"),
                    JYZSH = GetString(reader, "JYZSH"),
                    SJBYSJ = GetString(reader, "SJBYSJ"),
                    SFSYXW = GetString(reader, "SFSYXW"),
                    SYXW = GetString(reader, "SYXW"),
                    XWZH = GetString(reader, "XWZH"),
                    XWSYSJ = GetString(reader, "XWSYSJ"),
                    ZSZPB = GetBlob(reader, "ZSZPB"),
                    XWZPB = GetBlob(reader, "XWZPB"),
                    XJZPB = GetBlob(reader, "XJZPB"),
                    BYZPB = GetBlob(reader, "BYZPB"),
                    XZ = GetString(reader, "XZ"),
                    PYCC = GetString(reader, "PYCC"),
                    RXNY = GetString(reader, "RXNY"),
                    SFZC = GetString(reader, "SFZC"),
                    BISTUGPA = GetString(reader, "BISTUGPA"),
                    BYJMC = GetString(reader, "BYJMC"),
                    SYXWDM = GetString(reader, "SYXWDM")
                });
            }

            return list;
        }

        private static string? GetString(OracleDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString();
        }

        private static byte[]? GetBlob(OracleDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            if (reader.GetOracleValue(ordinal) is OracleBlob blob)
            {
                using (blob)
                {
                    if (blob.IsNull || blob.Length == 0)
                        return null;

                    var buffer = new byte[blob.Length];
                    blob.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }

            using var ms = new MemoryStream();
            const int bufferSize = 81920;
            long dataIndex = 0;
            var bufferBytes = new byte[bufferSize];
            long bytesRead;
            while ((bytesRead = reader.GetBytes(ordinal, dataIndex, bufferBytes, 0, bufferBytes.Length)) > 0)
            {
                ms.Write(bufferBytes, 0, (int)bytesRead);
                dataIndex += bytesRead;
            }
            return ms.Length == 0 ? null : ms.ToArray();
        }
    }
}
