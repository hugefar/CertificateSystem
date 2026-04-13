using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace CertificateSystem.Model
{
    /// <summary>
    /// 打印记录表实体
    /// </summary>
    public class PrintRecord
    {
        /// <summary>
        /// 打印时间
        /// </summary>
        public DateTime PrintTime { get; set; }

        /// <summary>
        /// 所属部门
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 证书类型
        /// </summary>
        public string CertificateType { get; set; }

        /// <summary>
        /// 学生姓名
        /// </summary>
        public string StudentName { get; set; }

        /// <summary>
        /// 学号
        /// </summary>
        public string StudentNo { get; set; }

        /// <summary>
        /// 操作用户名
        /// </summary>
        public string OperatorName { get; set; }

        /// <summary>
        /// 备注（可选，如“批量打印/单个打印”）
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}