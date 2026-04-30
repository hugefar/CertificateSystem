using CertificateSystem.Model;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CertificateSystem.DAL
{
    public class OraclePaperRepository : IOraclePaperRepository
    {
        private readonly string _connectionString;

        public OraclePaperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<OraclePaperRawDto>> GetAllPapersAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT ID, YEAR, STU_NO, STU_NAME, TEACHER_NO, TEACHER_NAME,
       GRADE, CLASS_CODE, CLASS_NAME, SPEC_CODE, SPEC_NAME, DEP_NAME,
       GRAD_TYPE_NAME, GRAD_NAME, KEYWORDS, PAPER_SOURCE_NAME, DIRECTION,
       LABGUAGE_NAME, FINAL_SCORE, TEA_NAMES
FROM zxtd_bslwxx";

            var list = new List<OraclePaperRawDto>();
            await using var conn = new OracleConnection(_connectionString);
            await using var cmd = new OracleCommand(sql, conn)
            {
                BindByName = true
            };

            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new OraclePaperRawDto
                {
                    ID = GetString(reader, "ID"),
                    YEAR = GetString(reader, "YEAR"),
                    STU_NO = GetString(reader, "STU_NO"),
                    STU_NAME = GetString(reader, "STU_NAME"),
                    TEACHER_NO = GetString(reader, "TEACHER_NO"),
                    TEACHER_NAME = GetString(reader, "TEACHER_NAME"),
                    GRADE = GetString(reader, "GRADE"),
                    CLASS_CODE = GetString(reader, "CLASS_CODE"),
                    CLASS_NAME = GetString(reader, "CLASS_NAME"),
                    SPEC_CODE = GetString(reader, "SPEC_CODE"),
                    SPEC_NAME = GetString(reader, "SPEC_NAME"),
                    DEP_NAME = GetString(reader, "DEP_NAME"),
                    GRAD_TYPE_NAME = GetString(reader, "GRAD_TYPE_NAME"),
                    GRAD_NAME = GetClobString(reader, "GRAD_NAME"),
                    KEYWORDS = GetClobString(reader, "KEYWORDS"),
                    PAPER_SOURCE_NAME = GetString(reader, "PAPER_SOURCE_NAME"),
                    DIRECTION = GetClobString(reader, "DIRECTION"),
                    LABGUAGE_NAME = GetString(reader, "LABGUAGE_NAME"),
                    FINAL_SCORE = GetDecimal(reader, "FINAL_SCORE"),
                    TEA_NAMES = GetClobString(reader, "TEA_NAMES")
                });
            }

            return list;
        }

        private static string? GetString(OracleDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString()?.Trim();
        }

        private static string? GetClobString(OracleDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var oracleValue = reader.GetOracleValue(ordinal);
            if (oracleValue is OracleClob clob)
            {
                using (clob)
                {
                    return clob.IsNull ? null : clob.Value?.Trim();
                }
            }

            if (oracleValue is OracleString oracleString)
            {
                return oracleString.IsNull ? null : oracleString.Value?.Trim();
            }

            return reader.GetValue(ordinal)?.ToString()?.Trim();
        }

        private static decimal? GetDecimal(OracleDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return decimal.TryParse(value?.ToString(), out var result) ? result : null;
        }
    }
}
