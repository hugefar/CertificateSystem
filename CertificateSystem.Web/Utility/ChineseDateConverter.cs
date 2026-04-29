using System;
using System.Text;

namespace CertificateSystem.Web.Utility
{
    
    public static class ChineseDateConverter
    {
        // 数字0-9对应的中文字符，0用“〇”
        private static readonly char[] ChineseDigits =
            { '〇', '一', '二', '三', '四', '五', '六', '七', '八', '九' };

        /// <summary>
        /// 将 DateTime 转换为证书常用中文大写日期格式，如“二〇二六年一月十九日”
        /// </summary>
        public static string ToChineseDate(this DateTime date)
        {
            string year = ConvertYear(date.Year);
            string month = ConvertMonth(date.Month);
            string day = ConvertDay(date.Day);
            return $"{year}年{month}月{day}日";
        }

        public static string ToChineseDate(this DateTime? nullableDate)
        {
            if (nullableDate == null)
                return string.Empty; // 或者抛出异常，视需求而定
            return nullableDate.Value.ToChineseDate();
        }

        // 年份转换：2026 -> "二〇二六"
        private static string ConvertYear(int year)
        {
            string yearStr = year.ToString();
            StringBuilder sb = new StringBuilder();
            foreach (char c in yearStr)
            {
                int digit = c - '0';
                sb.Append(ChineseDigits[digit]);
            }
            return sb.ToString();
        }

        // 月份转换：1 -> "一", 10 -> "十", 12 -> "十二"
        private static string ConvertMonth(int month)
        {
            return ConvertNumberToChinese(month);
        }

        // 日期转换：1 -> "一", 10 -> "十", 21 -> "二十一", 30 -> "三十"
        private static string ConvertDay(int day)
        {
            return ConvertNumberToChinese(day);
        }

        // 将1-31的数字转换为中文（不带“年、月、日”）
        private static string ConvertNumberToChinese(int num)
        {
            if (num < 1 || num > 31)
                throw new ArgumentOutOfRangeException(nameof(num), "数值应在1-31之间");

            int tens = num / 10;
            int ones = num % 10;

            if (tens == 0)          // 1-9
            {
                return ChineseDigits[ones].ToString();
            }
            if (tens == 1)          // 10-19
            {
                return "十" + (ones == 0 ? "" : ChineseDigits[ones].ToString());
            }
            if (tens == 2)          // 20-29
            {
                return "二十" + (ones == 0 ? "" : ChineseDigits[ones].ToString());
            }
            // tens == 3 (30, 31)
            return "三十" + (ones == 0 ? "" : ChineseDigits[ones].ToString());
        }
    }
}
