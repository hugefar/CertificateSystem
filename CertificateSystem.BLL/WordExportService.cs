using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Spire.Doc.Formatting;
using CertificateSystem.Model;

namespace CertificateSystem.BLL
{
    public class WordExportService : IWordExportService
    {
        public byte[] ExportDegreeWord(List<StudentCertificate> students)
        {
            return ExportWord(students, "普通高等教育学士学位授予人员名单");
        }

        public byte[] ExportSecondDegreeWord(List<StudentCertificate> students)
        {
            return ExportWord(students, "第二学士学位授予人员名单");
        }

        private static byte[] ExportWord(List<StudentCertificate> students, string mainTitle)
        {
            using var document = new Document();
            var section = document.AddSection();
            SetPageSetup(section);

            AddConfirmTitle(section, "北京学位中心纸质手册数据工作确认单");
            AddConfirmTable(section);
            AddBlankParagraph(section, 2);
            AddCenterTitle(section, "北京学位中心纸质手册模板", "宋体", 16f, true);
            AddBlankParagraph(section, 1);
            AddCenterTitle(section, mainTitle, "宋体", 16f, true);
            AddBlankParagraph(section, 1);

            var institutes = students
                .Where(x => !string.IsNullOrWhiteSpace(x.Institute) && !string.IsNullOrWhiteSpace(x.Major))
                .GroupBy(x => x.Institute)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var instituteGroup in institutes)
            {
                AddCenterTitle(section, instituteGroup.Key ?? string.Empty, "宋体", 12f, true);

                var majors = instituteGroup
                    .GroupBy(x => x.Major)
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                for (var i = 0; i < majors.Count; i++)
                {
                    var majorGroup = majors[i];
                    var studentsInMajor = majorGroup
                        .OrderBy(x => x.StudentId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var majorTitle = $"{ToChineseNumber(i + 1)}、{majorGroup.Key}（{studentsInMajor.Count}人）";
                    AddLeftTitle(section, majorTitle, "宋体", 12f, false);
                    AddStudentTable(section, studentsInMajor);
                    AddBlankParagraph(section, 1);
                }
            }

            using var stream = new MemoryStream();
            document.SaveToStream(stream, FileFormat.Docx);
            return stream.ToArray();
        }

        private static void SetPageSetup(Section section)
        {
            section.PageSetup.PageSize = PageSize.A4;
            section.PageSetup.Orientation = PageOrientation.Portrait;
            section.PageSetup.Margins.Top = CmToPoint(2.54f);
            section.PageSetup.Margins.Bottom = CmToPoint(2.54f);
            section.PageSetup.Margins.Left = CmToPoint(3.18f);
            section.PageSetup.Margins.Right = CmToPoint(3.18f);
        }

        private static void AddConfirmTitle(Section section, string text)
        {
            AddCenterTitle(section, text, "宋体", 16f, true);
            AddBlankParagraph(section, 1);
        }

        private static void AddCenterTitle(Section section, string text, string fontName, float fontSize, bool bold)
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.HorizontalAlignment = HorizontalAlignment.Center;
            var range = paragraph.AppendText(text);
            ApplyCharacterFormat(range.CharacterFormat, fontName, fontSize, bold);
        }

        private static void AddLeftTitle(Section section, string text, string fontName, float fontSize, bool bold)
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.HorizontalAlignment = HorizontalAlignment.Left;
            var range = paragraph.AppendText(text);
            ApplyCharacterFormat(range.CharacterFormat, fontName, fontSize, bold);
        }

        private static void AddBlankParagraph(Section section, int count)
        {
            for (var i = 0; i < count; i++)
            {
                section.AddParagraph();
            }
        }

        private static void AddConfirmTable(Section section)
        {
            var table = section.AddTable(true);
            table.ResetCells(2, 4);
            table.TableFormat.Borders.BorderType = BorderStyle.Single;
            table.TableFormat.HorizontalAlignment = RowAlignment.Center;

            var headers = new[] { "序号", "工作内容", "数据报送人签字", "部门负责人" };
            for (var i = 0; i < headers.Length; i++)
            {
                SetCellText(table.Rows[0].Cells[i], headers[i], "宋体", 12f, true, HorizontalAlignment.Center);
            }

            SetCellText(table.Rows[1].Cells[0], "1", "宋体", 12f, false, HorizontalAlignment.Center);
            SetCellText(table.Rows[1].Cells[1], "普通高等教育学士学位授予人员名单", "宋体", 12f, false, HorizontalAlignment.Left);
            SetCellText(table.Rows[1].Cells[2], "", "宋体", 12f, false, HorizontalAlignment.Center);
            SetCellText(table.Rows[1].Cells[3], "", "宋体", 12f, false, HorizontalAlignment.Center);

            table.Rows[0].Height = 28;
            table.Rows[1].Height = 40;
            table.AutoFit(AutoFitBehaviorType.AutoFitToContents);
        }

        private static void AddStudentTable(Section section, List<StudentCertificate> students)
        {
            var table = section.AddTable(true);  
            table.ResetCells(students.Count + 1, 6);
            table.TableFormat.Borders.BorderType = BorderStyle.Single;
            table.TableFormat.HorizontalAlignment = RowAlignment.Center;

            var headers = new[] { "序号", "姓名", "性别", "身份证号", "学位证书号", "学位类别" };
            for (var i = 0; i < headers.Length; i++)
            {
                SetCellText(table.Rows[0].Cells[i], headers[i], "宋体", 10f, false, HorizontalAlignment.Center);
            }

            for (var i = 0; i < students.Count; i++)
            {
                var student = students[i];
                SetCellText(table.Rows[i + 1].Cells[0], (i + 1).ToString(), "宋体", 11f, false, HorizontalAlignment.Center); 
                SetCellText(table.Rows[i + 1].Cells[1], student.Name, "宋体", 11f, false, HorizontalAlignment.Left);
                SetCellText(table.Rows[i + 1].Cells[2], student.Gender ?? string.Empty, "宋体", 11f, false, HorizontalAlignment.Left);
                SetCellText(table.Rows[i + 1].Cells[3], student.IdCardNo ?? string.Empty, "宋体", 11f, false, HorizontalAlignment.Left);
                SetCellText(table.Rows[i + 1].Cells[4], student.DegreeCertificateNumber ?? string.Empty, "宋体", 11f, false, HorizontalAlignment.Center);
                SetCellText(table.Rows[i + 1].Cells[5], student.AwardedDegree ?? string.Empty, "宋体", 11f, false, HorizontalAlignment.Left);
            }

            //设置表格各列的宽度
            //table.ColumnWidth = new[] { 5f, 10f, 20f, 40f, 40f, 40f };

            //table.Rows[0].Height = 24;
            //for (var i = 1; i < table.Rows.Count; i++)
            //{
            //    table.Rows[i].Height = 22;
            //}

            //table.AutoFit(AutoFitBehaviorType.AutoFitToWindow);

            // 遍历所有行，设置指定列的宽度
            for (int i = 0; i < table.Rows.Count; i++)
            {
                // 设置第1列（索引0）宽度为80磅
                table.Rows[i].Cells[0].SetCellWidth(35f, CellWidthType.Point);//学号
                table.Rows[i].Cells[2].SetCellWidth(35f, CellWidthType.Point);//性别
                table.Rows[i].Cells[3].SetCellWidth(100f, CellWidthType.Point);//身份证号
                table.Rows[i].Cells[4].SetCellWidth(100f, CellWidthType.Point);//学位证书号
                table.Rows[i].Cells[5].SetCellWidth(70f, CellWidthType.Point);//学位类别

            }
        }

        private static void SetCellText(TableCell cell, string text, string fontName, float fontSize, bool bold, HorizontalAlignment alignment)
        {
            cell.CellFormat.VerticalAlignment = VerticalAlignment.Middle;
            var paragraph = cell.AddParagraph();
            paragraph.Format.HorizontalAlignment = alignment;
            var range = paragraph.AppendText(text ?? string.Empty);
            ApplyCharacterFormat(range.CharacterFormat, fontName, fontSize, bold);
        }

        private static void ApplyCharacterFormat(CharacterFormat format, string fontName, float fontSize, bool bold)
        {
            format.FontName = fontName;
            format.FontSize = fontSize;
            format.Bold = bold;
        }

        private static float CmToPoint(float cm)
        {
            return cm * 28.35f;
        }

        private static string ToChineseNumber(int number)
        {
            return number switch
            {
                1 => "一",
                2 => "二",
                3 => "三",
                4 => "四",
                5 => "五",
                6 => "六",
                7 => "七",
                8 => "八",
                9 => "九",
                10 => "十",
                11 => "十一",
                12 => "十二",
                13 => "十三",
                14 => "十四",
                15 => "十五",
                16 => "十六",
                17 => "十七",
                18 => "十八",
                19 => "十九",
                20 => "二十",
                _ => number.ToString()
            };
        }
    }
}
