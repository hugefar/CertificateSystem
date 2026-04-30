using CertificateSystem.DAL;
using CertificateSystem.Web.Utility;
using Spire.Doc;
using Spire.Doc.Documents;
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
                    //    string[] fieldNames = new string[]
                    //    {
                    //"Name", "Gender", "BirthYear", "BirthMonth", "BirthDay",
                    //"EnrollYear","EnrollMonth", "GraduationYear","GraduationMonth","GraduationDay", "Major", "EducationLength",
                    //"EducationLevel", "IdCardNo", "CertificateNo"
                    //    };
                    //    string[] fieldValues = new string[]
                    //    {
                    //entity.Name, entity.Gender, "2002","5","18",
                    //"2020","9", "2026","1","19", "计算机科学与技术", "四",
                    //"本", "112321202505001048", "112321202505001048"
                    //    };
                    //    document.MailMerge.Execute(fieldNames, fieldValues);

                    // 替换占位符的方式已改为邮件合并，以下代码已注释掉，但请勿删除，以备后续参考
                    ReplacePlaceholder(document, "[XM]", entity.Name);
                    ReplacePlaceholder(document, "[XB]", entity.Gender);
                    ReplacePlaceholder(document, "[CSN]", entity.BirthDate?.ToString("yyyy") ?? string.Empty);
                    ReplacePlaceholder(document, "[CSY]", entity.BirthDate?.ToString("%M") ?? string.Empty);
                    ReplacePlaceholder(document, "[CSR]", entity.BirthDate?.ToString("%d") ?? string.Empty);
                    ReplacePlaceholder(document, "[RXN]", entity.EnrollmentDate?.ToString("yyyy") ?? string.Empty);
                    ReplacePlaceholder(document, "[RXY]", entity.EnrollmentDate?.ToString("%M") ?? string.Empty);
                    ReplacePlaceholder(document, "[BYN]", entity.GraduationDate?.ToString("yyyy") ?? string.Empty);
                    ReplacePlaceholder(document, "[BYY]", entity.GraduationDate?.ToString("%M") ?? string.Empty);
                    ReplacePlaceholder(document, "[BYR]", entity.GraduationDate?.ToString("%d") ?? string.Empty);
                    ReplacePlaceholder(document, "[ZY]", entity.Major);
                    ReplacePlaceholder(document, "[XZ]", GetChineseSchoolYear(entity.StudyYears ?? 0));
                    ReplacePlaceholder(document, "[CC]", entity.EducationLevel.Replace("本","") ?? string.Empty);
                    ReplacePlaceholder(document, "[ZSBH]", entity.CertificateNumber ?? string.Empty);
                    ReplacePlaceholder(document, "[XWLX]", entity.AwardedDegree);
                    ReplacePlaceholder(document, "[XWBH]", entity.DegreeCertificateNumber);
                    ReplacePlaceholder(document, "[HXWRQ]", entity.GraduationDate.ToChineseDate());

                    //测试照片地址
                    //entity.PhotoPath = @"J:\2026TEMP\20260422\U202510114.JPG";
                    byte[] xszpBytes = entity.ZSZPB!=null? entity.ZSZPB: (entity.BYZPB!=null? entity.BYZPB : entity.XJZPB); //ToolHelper.GetPictureData(entity.PhotoPath);
                    ReplaceImageByPicTitle(document, xszpBytes);    


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

            doc.Replace(placeholder, value ?? string.Empty, false, false);
        }

        /// 尝试在文档中插入学生照片，路径来自数据库。照片位置由模板中的书签 StudentPhoto 决定。
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
                    pic.Width = 2.62f;    // 厘米
                    pic.Height = 3.5f;

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

        // 单位转换常量：1厘米 = 28.35磅（Word/Spire.Doc默认单位）
        private const float CmToPoint = 28.35f;
        // 毕业证1寸证件照标准尺寸：高3.5cm，宽2.62cm
        private const float CertPhotoHeight = 3.5f * CmToPoint;
        private const float CertPhotoWidth = 2.62f * CmToPoint;
        //方法二：通过图片名称替换（简单场景用）
        public void ReplaceImageByPicTitle(Spire.Doc.Document document, byte[] xszpBytes)
        {
            // 空值校验，避免运行时报错
            if (document == null || xszpBytes == null || xszpBytes.Length == 0)
                throw new ArgumentException("文档或图片数据不能为空");

            // 遍历文档所有段落
            foreach (Spire.Doc.Section section in document.Sections)
            {
                foreach (Spire.Doc.Documents.Paragraph paragraph in section.Paragraphs)
                {
                    foreach (Spire.Doc.DocumentObject docObj in paragraph.ChildObjects)
                    {
                        if (docObj.DocumentObjectType == Spire.Doc.Documents.DocumentObjectType.Picture)
                        {
                            Spire.Doc.Fields.DocPicture picture = docObj as Spire.Doc.Fields.DocPicture;

                            // 核心修改：把 Name 改为 AlternativeText，对应Word里的可选文字标题
                            if (!string.IsNullOrEmpty(picture.AlternativeText) && picture.AlternativeText.Trim() == "StudentPhtoto")
                            {
                                // 1. 先保存原占位图的宽高（注意：这里保存的是磅值）
                                float originalWidth = picture.Width;
                                float originalHeight = picture.Height;

                              
                                // 原地替换图片
                                using (MemoryStream ms = new MemoryStream(xszpBytes))
                                {
                                    picture.LoadImage(ms);
                                }
                                //// 恢复原尺寸
                                //picture.Width = width;
                                //picture.Height = height;

                                // 强制锁定毕业证标准尺寸，彻底解决变形
                                picture.Width = originalWidth;
                                picture.Height = originalHeight;
                                 
                                return; // 替换完成直接退出，无需继续遍历
                            }
                        }
                    }
                }
            }
            // 未找到匹配图片的提示，方便调试
            //throw new Exception("未找到可选文字标题为 'StudentPhtoto' 的图片，请检查模板引擎设置");
        }

        public static string GetChineseSchoolYear(int schoolYear)
        {
            // 索引0留空，让1-4直接对应"一"-"四"
            string[] chineseNumbers = { "", "一", "二", "三", "四" };

            // 先验证输入范围，避免数组越界
            if (schoolYear < 1 || schoolYear > 4)
            {
                return "未知学制"; // 或返回 string.Empty，根据业务需求调整
            }

            return chineseNumbers[schoolYear];
        }
    }
}
