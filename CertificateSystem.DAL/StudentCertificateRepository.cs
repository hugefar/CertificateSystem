using System.Data;
using System.Data.SqlClient;
using System.Text;
using CertificateSystem.DBUtility;
using CertificateSystem.Model;

namespace CertificateSystem.DAL
{
    public interface IStudentCertificateRepository
    {
        Task<PageResult<StudentCertificate>> GetPagedListAsync(StudentCertificateQueryDto query);
        Task<StudentCertificate> GetByIdAsync(long id);
        Task<List<StudentCertificate>> GetListByFilterAsync(StudentCertificateQueryDto filter, int? top = null);
        Task<int> CountByFilterAsync(StudentCertificateQueryDto filter);
        Task<int> BatchInsertAsync(IEnumerable<StudentCertificate> list);
        Task<int> UpdateAsync(StudentCertificate entity);
        Task<int> DeleteAsync(long id);
        // Distinct value queries for cascading selects
        Task<List<string>> GetDistinctGraduationYearsAsync(string certificateType);
        Task<List<string>> GetDistinctInstitutesAsync(string certificateType, string graduationYear);
        Task<List<string>> GetDistinctMajorsAsync(string certificateType, string graduationYear, string institute);
        Task<List<string>> GetDistinctClassesAsync(string certificateType, string graduationYear, string institute, string major);
    }

    public class StudentCertificateRepository : IStudentCertificateRepository
    {
        public async Task<PageResult<StudentCertificate>> GetPagedListAsync(StudentCertificateQueryDto query)
        {
            query ??= new StudentCertificateQueryDto();
            if (query.PageIndex <= 0) query.PageIndex = 1;
            if (query.PageSize <= 0) query.PageSize = 20;

            var parameters = new List<SqlParameter>();
            var whereSql = BuildWhereSql(query, parameters);

            var countSql = $"SELECT COUNT(1) FROM dbo.StudentCertificates {whereSql}";

            var pageSql = $@"
SELECT Id, CertificateType, GraduationYear, GraduationYearName, Institute, Major, ClassName, StudentId, Name, Gender,
       Nation, PoliticalStatus, IdCardType, IdCardNo, ExamNo, StudyMode,
       BirthDate, EnrollmentDate, GraduationDate, GraduationConclusion, StudyYears, EducationLevel,
       CertificateNumber, CertificateDate, PhotoPath, IsDegreeAwarded, AwardedDegree, DegreeCertificateNumber,
       DegreeAwardDate, Gpa, IsRegistered, AwardedDegreeCode, ZSZPB, XWZPB, XJZPB, BYZPB,
       CreatedAt, UpdatedAt, SyncBatchId
FROM dbo.StudentCertificates
{whereSql}
ORDER BY GraduationYear DESC, Institute ASC, Major ASC, ClassName ASC, StudentId ASC
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

            var items = new List<StudentCertificate>();
            await using (var cmd = new SqlCommand(pageSql, conn))
            {
                cmd.Parameters.AddRange(CloneParameters(parameters));
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(MapStudentCertificate(reader));
                }
            }

            return new PageResult<StudentCertificate>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<StudentCertificate> GetByIdAsync(long id)
        {
            const string sql = @"
SELECT Id, CertificateType, GraduationYear, GraduationYearName, Institute, Major, ClassName, StudentId, Name, Gender,
       Nation, PoliticalStatus, IdCardType, IdCardNo, ExamNo, StudyMode,
       BirthDate, EnrollmentDate, GraduationDate, GraduationConclusion, StudyYears, EducationLevel,
       CertificateNumber, CertificateDate, PhotoPath, IsDegreeAwarded, AwardedDegree, DegreeCertificateNumber,
       DegreeAwardDate, Gpa, IsRegistered, AwardedDegreeCode, ZSZPB, XWZPB, XJZPB, BYZPB,
       CreatedAt, UpdatedAt, SyncBatchId
FROM dbo.StudentCertificates
WHERE Id = @Id";

            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Id", id));
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapStudentCertificate(reader);
            }

            throw new KeyNotFoundException($"StudentCertificate not found. Id={id}");
        }

        public async Task<List<StudentCertificate>> GetListByFilterAsync(StudentCertificateQueryDto filter, int? top = null)
        {
            filter ??= new StudentCertificateQueryDto();
            var parameters = new List<SqlParameter>();
            var whereSql = BuildWhereSql(filter, parameters);

            var topSql = top.HasValue && top.Value > 0 ? "TOP (@Top)" : string.Empty;
            var sql = $@"
SELECT {topSql} Id, CertificateType, GraduationYear, GraduationYearName, Institute, Major, ClassName, StudentId, Name, Gender,
       Nation, PoliticalStatus, IdCardType, IdCardNo, ExamNo, StudyMode,
       BirthDate, EnrollmentDate, GraduationDate, GraduationConclusion, StudyYears, EducationLevel,
       CertificateNumber, CertificateDate, PhotoPath, IsDegreeAwarded, AwardedDegree, DegreeCertificateNumber,
       DegreeAwardDate, Gpa, IsRegistered, AwardedDegreeCode, ZSZPB, XWZPB, XJZPB, BYZPB,
       CreatedAt, UpdatedAt, SyncBatchId
FROM dbo.StudentCertificates
{whereSql}
ORDER BY GraduationYear DESC, Institute ASC, Major ASC, ClassName ASC, StudentId ASC";

            if (top.HasValue && top.Value > 0)
                parameters.Add(new SqlParameter("@Top", top.Value));

            var list = new List<StudentCertificate>();
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(CloneParameters(parameters));
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapStudentCertificate(reader));
            }

            return list;
        }

        public async Task<int> CountByFilterAsync(StudentCertificateQueryDto filter)
        {
            filter ??= new StudentCertificateQueryDto();
            var parameters = new List<SqlParameter>();
            var whereSql = BuildWhereSql(filter, parameters);

            var sql = $"SELECT COUNT(1) FROM dbo.StudentCertificates {whereSql}";

            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(CloneParameters(parameters));
            await conn.OpenAsync();
            var scalar = await cmd.ExecuteScalarAsync();
            return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
        }

        // Distinct value queries for cascading filters
        public async Task<List<string>> GetDistinctGraduationYearsAsync(string certificateType)
        {
            const string sql = @"SELECT DISTINCT GraduationYear FROM dbo.StudentCertificates WHERE CertificateType = @CertificateType ORDER BY GraduationYear";
            var list = new List<string>();
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@CertificateType", certificateType));
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader[0]?.ToString() ?? string.Empty);
            }
            return list;
        }

        public async Task<List<string>> GetDistinctInstitutesAsync(string certificateType, string graduationYear)
        {
            const string sql = @"SELECT DISTINCT Institute FROM dbo.StudentCertificates WHERE CertificateType = @CertificateType AND (@GraduationYear IS NULL OR @GraduationYear = '' OR GraduationYear = @GraduationYear) ORDER BY Institute";
            var list = new List<string>();
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@CertificateType", certificateType));
            cmd.Parameters.Add(new SqlParameter("@GraduationYear", (object?)graduationYear ?? DBNull.Value));
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader[0]?.ToString() ?? string.Empty);
            }
            return list;
        }

        public async Task<List<string>> GetDistinctMajorsAsync(string certificateType, string graduationYear, string institute)
        {
            const string sql = @"SELECT DISTINCT Major FROM dbo.StudentCertificates WHERE CertificateType = @CertificateType AND (@GraduationYear IS NULL OR @GraduationYear = '' OR GraduationYear = @GraduationYear) AND (@Institute IS NULL OR @Institute = '' OR Institute = @Institute) ORDER BY Major";
            var list = new List<string>();
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@CertificateType", certificateType));
            cmd.Parameters.Add(new SqlParameter("@GraduationYear", (object?)graduationYear ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Institute", (object?)institute ?? DBNull.Value));
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader[0]?.ToString() ?? string.Empty);
            }
            return list;
        }

        public async Task<List<string>> GetDistinctClassesAsync(string certificateType, string graduationYear, string institute, string major)
        {
            const string sql = @"SELECT DISTINCT ClassName FROM dbo.StudentCertificates WHERE CertificateType = @CertificateType AND (@GraduationYear IS NULL OR @GraduationYear = '' OR GraduationYear = @GraduationYear) AND (@Institute IS NULL OR @Institute = '' OR Institute = @Institute) AND (@Major IS NULL OR @Major = '' OR Major = @Major) ORDER BY ClassName";
            var list = new List<string>();
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@CertificateType", certificateType));
            cmd.Parameters.Add(new SqlParameter("@GraduationYear", (object?)graduationYear ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Institute", (object?)institute ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Major", (object?)major ?? DBNull.Value));
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader[0]?.ToString() ?? string.Empty);
            }
            return list;
        }

        public async Task<int> BatchInsertAsync(IEnumerable<StudentCertificate> list)
        {
            var entities = list?.ToList() ?? new List<StudentCertificate>();
            if (entities.Count == 0) return 0;

            const string sql = @"
INSERT INTO dbo.StudentCertificates
(CertificateType, GraduationYear, GraduationYearName, Institute, Major, ClassName, StudentId, Name, Gender,
 Nation, PoliticalStatus, IdCardType, IdCardNo, ExamNo, StudyMode,
 BirthDate, EnrollmentDate, GraduationDate, GraduationConclusion, StudyYears, EducationLevel,
 CertificateNumber, CertificateDate, PhotoPath, IsDegreeAwarded, AwardedDegree, DegreeCertificateNumber,
 DegreeAwardDate, Gpa, IsRegistered, AwardedDegreeCode, ZSZPB, XWZPB, XJZPB, BYZPB,
 CreatedAt, UpdatedAt, SyncBatchId)
VALUES
(@CertificateType, @GraduationYear, @GraduationYearName, @Institute, @Major, @ClassName, @StudentId, @Name, @Gender,
 @Nation, @PoliticalStatus, @IdCardType, @IdCardNo, @ExamNo, @StudyMode,
 @BirthDate, @EnrollmentDate, @GraduationDate, @GraduationConclusion, @StudyYears, @EducationLevel,
 @CertificateNumber, @CertificateDate, @PhotoPath, @IsDegreeAwarded, @AwardedDegree, @DegreeCertificateNumber,
 @DegreeAwardDate, @Gpa, @IsRegistered, @AwardedDegreeCode, @ZSZPB, @XWZPB, @XJZPB, @BYZPB,
 @CreatedAt, @UpdatedAt, @SyncBatchId)";

            await using var conn = SqlHelper.CreateConnection();
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();
            try
            {
                var affected = 0;
                foreach (var e in entities)
                {
                    await using var cmd = new SqlCommand(sql, conn, (SqlTransaction)tran);
                    FillEntityParameters(cmd, e, includeId: false);
                    affected += await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return affected;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<int> UpdateAsync(StudentCertificate entity)
        {
            const string sql = @"
UPDATE dbo.StudentCertificates
SET CertificateType = @CertificateType,
    GraduationYear = @GraduationYear,
    GraduationYearName = @GraduationYearName,
    Institute = @Institute,
    Major = @Major,
    ClassName = @ClassName,
    StudentId = @StudentId,
    Name = @Name,
    Gender = @Gender,
    Nation = @Nation,
    PoliticalStatus = @PoliticalStatus,
    IdCardType = @IdCardType,
    IdCardNo = @IdCardNo,
    ExamNo = @ExamNo,
    StudyMode = @StudyMode,
    BirthDate = @BirthDate,
    EnrollmentDate = @EnrollmentDate,
    GraduationDate = @GraduationDate,
    GraduationConclusion = @GraduationConclusion,
    StudyYears = @StudyYears,
    EducationLevel = @EducationLevel,
    CertificateNumber = @CertificateNumber,
    CertificateDate = @CertificateDate,
    PhotoPath = @PhotoPath,
    IsDegreeAwarded = @IsDegreeAwarded,
    AwardedDegree = @AwardedDegree,
    DegreeCertificateNumber = @DegreeCertificateNumber,
    DegreeAwardDate = @DegreeAwardDate,
    Gpa = @Gpa,
    IsRegistered = @IsRegistered,
    AwardedDegreeCode = @AwardedDegreeCode,
    ZSZPB = @ZSZPB,
    XWZPB = @XWZPB,
    XJZPB = @XJZPB,
    BYZPB = @BYZPB,
    UpdatedAt = @UpdatedAt,
    SyncBatchId = @SyncBatchId
WHERE Id = @Id";

            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            FillEntityParameters(cmd, entity, includeId: true);
            cmd.Parameters["@UpdatedAt"].Value = DateTime.Now;
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> DeleteAsync(long id)
        {
            const string sql = "DELETE FROM dbo.StudentCertificates WHERE Id = @Id";
            await using var conn = SqlHelper.CreateConnection();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Id", id));
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        private static string BuildWhereSql(StudentCertificateQueryDto query, List<SqlParameter> parameters)
        {
            var sb = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(query.CertificateType))
            {
                sb.Append(" AND CertificateType = @CertificateType");
                parameters.Add(new SqlParameter("@CertificateType", query.CertificateType));
            }

            if (!string.IsNullOrWhiteSpace(query.GraduationYear))
            {
                sb.Append(" AND GraduationYear = @GraduationYear");
                parameters.Add(new SqlParameter("@GraduationYear", query.GraduationYear));
            }

            if (!string.IsNullOrWhiteSpace(query.Institute))
            {
                sb.Append(" AND Institute = @Institute");
                parameters.Add(new SqlParameter("@Institute", query.Institute));
            }

            if (!string.IsNullOrWhiteSpace(query.Major))
            {
                sb.Append(" AND Major = @Major");
                parameters.Add(new SqlParameter("@Major", query.Major));
            }

            if (!string.IsNullOrWhiteSpace(query.ClassName))
            {
                sb.Append(" AND ClassName = @ClassName");
                parameters.Add(new SqlParameter("@ClassName", query.ClassName));
            }

            if (!string.IsNullOrWhiteSpace(query.StudentIdOrName))
            {
                sb.Append(" AND (StudentId LIKE '%' + @Keyword + '%' OR Name LIKE '%' + @Keyword + '%')");
                parameters.Add(new SqlParameter("@Keyword", query.StudentIdOrName));
            }

            return sb.ToString();
        }

        private static StudentCertificate MapStudentCertificate(SqlDataReader reader)
        {
            return new StudentCertificate
            {
                Id = Convert.ToInt64(reader["Id"]),
                CertificateType = reader["CertificateType"]?.ToString() ?? string.Empty,
                GraduationYear = reader["GraduationYear"]?.ToString() ?? string.Empty,
                GraduationYearName = reader["GraduationYearName"] == DBNull.Value ? null : reader["GraduationYearName"].ToString(),
                Institute = reader["Institute"]?.ToString() ?? string.Empty,
                Major = reader["Major"]?.ToString() ?? string.Empty,
                ClassName = reader["ClassName"]?.ToString() ?? string.Empty,
                StudentId = reader["StudentId"]?.ToString() ?? string.Empty,
                Name = reader["Name"]?.ToString() ?? string.Empty,
                Gender = reader["Gender"] == DBNull.Value ? null : reader["Gender"].ToString(),
                Nation = reader["Nation"] == DBNull.Value ? null : reader["Nation"].ToString(),
                PoliticalStatus = reader["PoliticalStatus"] == DBNull.Value ? null : reader["PoliticalStatus"].ToString(),
                IdCardType = reader["IdCardType"] == DBNull.Value ? null : reader["IdCardType"].ToString(),
                IdCardNo = reader["IdCardNo"] == DBNull.Value ? null : reader["IdCardNo"].ToString(),
                ExamNo = reader["ExamNo"] == DBNull.Value ? null : reader["ExamNo"].ToString(),
                StudyMode = reader["StudyMode"] == DBNull.Value ? null : reader["StudyMode"].ToString(),
                BirthDate = reader["BirthDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["BirthDate"]),
                EnrollmentDate = reader["EnrollmentDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EnrollmentDate"]),
                GraduationDate = reader["GraduationDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["GraduationDate"]),
                GraduationConclusion = reader["GraduationConclusion"] == DBNull.Value ? null : reader["GraduationConclusion"].ToString(),
                StudyYears = reader["StudyYears"] == DBNull.Value ? null : Convert.ToInt32(reader["StudyYears"]),
                EducationLevel = reader["EducationLevel"] == DBNull.Value ? null : reader["EducationLevel"].ToString(),
                CertificateNumber = reader["CertificateNumber"] == DBNull.Value ? null : reader["CertificateNumber"].ToString(),
                CertificateDate = reader["CertificateDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["CertificateDate"]),
                PhotoPath = reader["PhotoPath"] == DBNull.Value ? null : reader["PhotoPath"].ToString(),
                IsDegreeAwarded = reader["IsDegreeAwarded"] == DBNull.Value ? null : reader["IsDegreeAwarded"].ToString(),
                AwardedDegree = reader["AwardedDegree"] == DBNull.Value ? null : reader["AwardedDegree"].ToString(),
                DegreeCertificateNumber = reader["DegreeCertificateNumber"] == DBNull.Value ? null : reader["DegreeCertificateNumber"].ToString(),
                DegreeAwardDate = reader["DegreeAwardDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DegreeAwardDate"]),
                Gpa = reader["Gpa"] == DBNull.Value ? null : reader["Gpa"].ToString(),
                IsRegistered = reader["IsRegistered"] == DBNull.Value ? null : reader["IsRegistered"].ToString(),
                AwardedDegreeCode = reader["AwardedDegreeCode"] == DBNull.Value ? null : reader["AwardedDegreeCode"].ToString(),
                ZSZPB = reader["ZSZPB"] == DBNull.Value ? null : (byte[])reader["ZSZPB"],
                XWZPB = reader["XWZPB"] == DBNull.Value ? null : (byte[])reader["XWZPB"],
                XJZPB = reader["XJZPB"] == DBNull.Value ? null : (byte[])reader["XJZPB"],
                BYZPB = reader["BYZPB"] == DBNull.Value ? null : (byte[])reader["BYZPB"],
                CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"]),
                SyncBatchId = reader["SyncBatchId"] == DBNull.Value ? null : reader["SyncBatchId"].ToString()
            };
        }

        private static SqlParameter[] CloneParameters(List<SqlParameter> source)
        {
            return source.Select(p => new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value)).ToArray();
        }

        private static void FillEntityParameters(SqlCommand cmd, StudentCertificate e, bool includeId)
        {
            if (includeId)
            {
                cmd.Parameters.Add(new SqlParameter("@Id", e.Id));
            }

            cmd.Parameters.Add(new SqlParameter("@CertificateType", e.CertificateType));
            cmd.Parameters.Add(new SqlParameter("@GraduationYear", e.GraduationYear));
            cmd.Parameters.Add(new SqlParameter("@GraduationYearName", (object?)e.GraduationYearName ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Institute", e.Institute));
            cmd.Parameters.Add(new SqlParameter("@Major", e.Major));
            cmd.Parameters.Add(new SqlParameter("@ClassName", e.ClassName));
            cmd.Parameters.Add(new SqlParameter("@StudentId", e.StudentId));
            cmd.Parameters.Add(new SqlParameter("@Name", e.Name));
            cmd.Parameters.Add(new SqlParameter("@Gender", (object?)e.Gender ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Nation", (object?)e.Nation ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@PoliticalStatus", (object?)e.PoliticalStatus ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IdCardType", (object?)e.IdCardType ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IdCardNo", (object?)e.IdCardNo ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ExamNo", (object?)e.ExamNo ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@StudyMode", (object?)e.StudyMode ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@BirthDate", (object?)e.BirthDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@EnrollmentDate", (object?)e.EnrollmentDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GraduationDate", (object?)e.GraduationDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GraduationConclusion", (object?)e.GraduationConclusion ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@StudyYears", (object?)e.StudyYears ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@EducationLevel", (object?)e.EducationLevel ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CertificateNumber", (object?)e.CertificateNumber ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CertificateDate", (object?)e.CertificateDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@PhotoPath", (object?)e.PhotoPath ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IsDegreeAwarded", (object?)e.IsDegreeAwarded ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@AwardedDegree", (object?)e.AwardedDegree ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DegreeCertificateNumber", (object?)e.DegreeCertificateNumber ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DegreeAwardDate", (object?)e.DegreeAwardDate ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Gpa", (object?)e.Gpa ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IsRegistered", (object?)e.IsRegistered ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@AwardedDegreeCode", (object?)e.AwardedDegreeCode ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ZSZPB", (object?)e.ZSZPB ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@XWZPB", (object?)e.XWZPB ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@XJZPB", (object?)e.XJZPB ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@BYZPB", (object?)e.BYZPB ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CreatedAt", e.CreatedAt == default ? DateTime.Now : e.CreatedAt));
            cmd.Parameters.Add(new SqlParameter("@UpdatedAt", (object?)e.UpdatedAt ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@SyncBatchId", (object?)e.SyncBatchId ?? DBNull.Value));
        }
    }
}
