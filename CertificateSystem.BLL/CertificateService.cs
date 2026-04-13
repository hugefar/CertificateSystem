using CertificateSystem.BLL.DataScope;
using CertificateSystem.DAL;
using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public interface ICertificateService
    {
        Task<PageResult<StudentCertificate>> GetPagedListAsync(StudentCertificateQueryDto query);
        Task<StudentCertificate> GetByIdAsync(long id);
        Task<List<StudentCertificate>> GetListByFilterAsync(StudentCertificateQueryDto filter, int? top = null);
        Task<int> CountByFilterAsync(StudentCertificateQueryDto filter);
        Task<BatchPrintTaskResultDto> CreateBatchPrintTaskAsync(StudentCertificateQueryDto filter, string certificateType, string? operatorName);

        // Distinct value helpers for cascading selects
        Task<List<string>> GetDistinctGraduationYearsAsync(string certificateType);
        Task<List<string>> GetDistinctInstitutesAsync(string certificateType, string? graduationYear);
        Task<List<string>> GetDistinctMajorsAsync(string certificateType, string? graduationYear, string? institute);
        Task<List<string>> GetDistinctClassesAsync(string certificateType, string? graduationYear, string? institute, string? major);
    }

    public class CertificateService : ICertificateService
    {
        private readonly IStudentCertificateRepository _repository;
        private readonly IDataScopeService _dataScopeService;

        public CertificateService(IStudentCertificateRepository repository, IDataScopeService dataScopeService)
        {
            _repository = repository;
            _dataScopeService = dataScopeService;
        }

        public async Task<List<string>> GetDistinctGraduationYearsAsync(string certificateType)
        {
            return await _repository.GetDistinctGraduationYearsAsync(certificateType);
        }

        public async Task<List<string>> GetDistinctInstitutesAsync(string certificateType, string? graduationYear)
        {
            // If user cannot access all institutes, only return the current user's institute
            var canAccessAll = await _dataScopeService.CanAccessAllInstitutesAsync();
            if (!canAccessAll)
            {
                var currentInstitute = await _dataScopeService.GetCurrentUserInstituteAsync();
                if (string.IsNullOrWhiteSpace(currentInstitute))
                    return new List<string>();

                return new List<string> { currentInstitute };
            }

            return await _repository.GetDistinctInstitutesAsync(certificateType, graduationYear ?? string.Empty);
        }

        public async Task<List<string>> GetDistinctMajorsAsync(string certificateType, string? graduationYear, string? institute)
        {
            return await _repository.GetDistinctMajorsAsync(certificateType, graduationYear ?? string.Empty, institute ?? string.Empty);
        }

        public async Task<List<string>> GetDistinctClassesAsync(string certificateType, string? graduationYear, string? institute, string? major)
        {
            return await _repository.GetDistinctClassesAsync(certificateType, graduationYear ?? string.Empty, institute ?? string.Empty, major ?? string.Empty);
        }

        public async Task<PageResult<StudentCertificate>> GetPagedListAsync(StudentCertificateQueryDto query)
        {
            query ??= new StudentCertificateQueryDto();

            var canAccessAll = await _dataScopeService.CanAccessAllInstitutesAsync();
            if (!canAccessAll)
            {
                var currentInstitute = await _dataScopeService.GetCurrentUserInstituteAsync();
                if (string.IsNullOrWhiteSpace(currentInstitute))
                {
                    return new PageResult<StudentCertificate>
                    {
                        Items = new List<StudentCertificate>(),
                        TotalCount = 0,
                        PageIndex = query.PageIndex,
                        PageSize = query.PageSize
                    };
                }

                query.Institute = currentInstitute;
            }

            return await _repository.GetPagedListAsync(query);
        }

        public async Task<StudentCertificate> GetByIdAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);

            var canAccessAll = await _dataScopeService.CanAccessAllInstitutesAsync();
            if (canAccessAll)
            {
                return entity;
            }

            var currentInstitute = await _dataScopeService.GetCurrentUserInstituteAsync();
            if (string.IsNullOrWhiteSpace(currentInstitute) ||
                !string.Equals(entity.Institute, currentInstitute, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("无权限访问该学院数据。");
            }

            return entity;
        }

        public async Task<List<StudentCertificate>> GetListByFilterAsync(StudentCertificateQueryDto filter, int? top = null)
        {
            filter ??= new StudentCertificateQueryDto();

            //var canAccessAll = await _dataScopeService.CanAccessAllInstitutesAsync();
            //if (!canAccessAll)
            //{
            //    var currentInstitute = await _dataScopeService.GetCurrentUserInstituteAsync();
            //    if (string.IsNullOrWhiteSpace(currentInstitute))
            //    {
            //        return new List<StudentCertificate>();
            //    }

            //    filter.Institute = currentInstitute;
            //}

            return await _repository.GetListByFilterAsync(filter, top);
        }

        public async Task<int> CountByFilterAsync(StudentCertificateQueryDto filter)
        {
            filter ??= new StudentCertificateQueryDto();

            //var canAccessAll = await _dataScopeService.CanAccessAllInstitutesAsync();

            //if (!canAccessAll)
            //{
            //    var currentInstitute = await _dataScopeService.GetCurrentUserInstituteAsync();
            //    if (string.IsNullOrWhiteSpace(currentInstitute))
            //    {
            //        return 0;
            //    }

            //    filter.Institute = currentInstitute;
            //}

            return await _repository.CountByFilterAsync(filter);
        }

        public async Task<BatchPrintTaskResultDto> CreateBatchPrintTaskAsync(StudentCertificateQueryDto filter, string certificateType, string? operatorName)
        {
            filter ??= new StudentCertificateQueryDto();
            filter.CertificateType = certificateType;

            var total = await CountByFilterAsync(filter);
            var taskId = Guid.NewGuid().ToString("N");

            return new BatchPrintTaskResultDto
            {
                TaskId = taskId,
                TotalCount = total,
                Message = $"批量打印任务已创建，操作人：{operatorName ?? "Unknown"}"
            };
        }
    }
}
