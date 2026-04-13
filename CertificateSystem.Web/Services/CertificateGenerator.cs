using CertificateSystem.DAL;
using Spire.Doc;
using Spire.Doc.Fields;

namespace CertificateSystem.Web.Services
{
    public class CertificateGenerator : ICertificateGenerator
    {
        private readonly IWebHostEnvironment _env;
        private readonly IStudentCertificateRepository _repository;
        private readonly ILogger<CertificateGenerator> _logger;

        public CertificateGenerator(
            IWebHostEnvironment env,
            IStudentCertificateRepository repository,
            ILogger<CertificateGenerator> logger)
        {
            _env = env;
            _repository = repository;
            _logger = logger;
        }

        public async Task<byte[]> GeneratePdfAsync(long studentId, string certificateType)
        {
            var entity = await _repository.GetByIdAsync(studentId);

            var templatePath = ResolveTemplatePath(certificateType, entity.Name);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"证书模板不存在：{templatePath}", templatePath);
            }

            try
            {
                return await Task.Run(() =>
                {
                    var document = new Spire.Doc.Document();
                    document.LoadFromFile(templatePath);


                    // 3. 邮件合并填充数据（和模板合并域一一对应）
                    string[] fieldNames = new string[]
                    {
                "Name", "Gender", "BirthYear", "BirthMonth", "BirthDay",
                "EnrollYear","EnrollMonth", "GraduationYear","GraduationMonth","GraduationDay", "Major", "EducationLength",
                "EducationLevel", "IdCardNo", "CertificateNo"
                    };
                    string[] fieldValues = new string[]
                    {
                entity.Name, entity.Gender, "2002","5","18",
                "2020","9", "2026","1","19", "计算机科学与技术", "四",
                "本", "112321202505001048", "112321202505001048"
                    };
                    document.MailMerge.Execute(fieldNames, fieldValues);

                    // 替换占位符的方式已改为邮件合并，以下代码已注释掉，但请勿删除，以备后续参考
                    //ReplacePlaceholder(document, "{Name}", entity.Name);
                    //ReplacePlaceholder(document, "{StudentId}", entity.StudentId);
                    //ReplacePlaceholder(document, "{Major}", entity.Major);
                    //ReplacePlaceholder(document, "{ClassName}", entity.ClassName);
                    //ReplacePlaceholder(document, "{Institute}", entity.Institute);
                    //ReplacePlaceholder(document, "{GraduationYear}", entity.GraduationYear);
                    //ReplacePlaceholder(document, "{CertificateNumber}", entity.CertificateNumber ?? string.Empty);
                    //ReplacePlaceholder(document, "{Gender}", entity.Gender ?? string.Empty);
                    //ReplacePlaceholder(document, "{BirthDate}", entity.BirthDate?.ToString("yyyy-MM-dd") ?? string.Empty);
                    //ReplacePlaceholder(document, "{EnrollmentDate}", entity.EnrollmentDate?.ToString("yyyy-MM-dd") ?? string.Empty);
                    //ReplacePlaceholder(document, "{GraduationDate}", entity.GraduationDate?.ToString("yyyy-MM-dd") ?? string.Empty);
                    //ReplacePlaceholder(document, "{EducationLevel}", entity.EducationLevel ?? string.Empty);

                    TryAppendPhoto(document, entity.PhotoPath);
 
                    using var ms = new MemoryStream();
                    document.SaveToStream(ms, FileFormat.PDF);
                    return ms.ToArray();
                });
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成证书PDF失败。StudentId={StudentId}, Type={Type}", studentId, certificateType);
                throw;
            }
        }

        private string ResolveTemplatePath(string certificateType, string studentName)
        {
            var typeKey = (certificateType ?? string.Empty).Trim().ToLowerInvariant();
            var isLongName = !string.IsNullOrWhiteSpace(studentName) && studentName.Length >= 4;

            string fileName = typeKey switch
            {
                "graduation" or "毕业" or "毕业证书" => isLongName ? "Graduation_multi-character.docx" : "Graduation.docx",
                "completion" or "结业" or "结业证书" => isLongName ? "Completion_multi-character.docx" : "Completion.docx",
                "degree" or "学位" or "学位证书" => "Degree.docx",
                "seconddegree" or "第二学位" or "第二学位证书" => "SecondDegree.docx",
                _ => throw new ArgumentException($"不支持的证书类型：{certificateType}")
            };

            return Path.Combine(_env.WebRootPath, "Templates", fileName);
        }

        private void ReplacePlaceholder(Spire.Doc.Document doc, string placeholder, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogWarning("占位符 {Placeholder} 未提供有效值，已使用空字符串。", placeholder);
            }

            doc.Replace(placeholder, value ?? string.Empty, true, true);
        }

        private void TryAppendPhoto(Spire.Doc.Document doc, string? photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath))
            {
                _logger.LogWarning("学生照片路径为空，跳过照片插入。");
                return;
            }

            var fullPath = photoPath.StartsWith("/")
                ? Path.Combine(_env.WebRootPath, photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))
                : photoPath;

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("学生照片文件不存在，跳过照片插入。Path={Path}", fullPath);
                return;
            }

            try
            {
                // ===================== 新增：插入学生照片 =====================
                // 定位Word中的书签 StudentPhoto（和你模板设置的书签名完全一致）
                Bookmark bookmark = doc.Bookmarks["StudentPhoto"];
                if (bookmark != null)
                {
                    // 在书签位置插入照片
                    DocPicture pic = new DocPicture(doc);
                    pic.LoadImage(fullPath);

                    // 严格匹配模板占位图尺寸（1寸照，防止拉伸）
                    pic.Width = 3.5f;    // 厘米
                    pic.Height = 4.5f;

                    // 替换书签位置的占位图
                    bookmark.BookmarkStart.OwnerParagraph.ChildObjects.Insert(0, pic);
                    // 删除原占位图（可选）
                    bookmark.BookmarkStart.OwnerParagraph.ChildObjects.RemoveAt(1);
                }
                // ==============================================================
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "插入学生照片失败，继续生成文档。");
            }
        }
    }
}
