using Blog.Core.Model.Models.RootTkey;
using SqlSugar;

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 血糖管理实例日志
    /// </summary>
    public class NightscoutLog : BaseEntity
    {
        /// <summary>
        /// 血糖实例父级ID
        /// </summary>
        public long pid { get; set; }

        /// <summary>
        /// 运行日志
        /// </summary>
        [SugarColumn(Length = 2000, IsNullable = true)]
        public string content { get; set; }
        /// <summary>
        /// 是否运行成功
        /// </summary>
         
        public bool success { get; set; }
    }
}
