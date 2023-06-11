using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blog.Core.Model.Models.RootTkey;
using SqlSugar;

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 微信公众号关键词回复
    /// </summary>
    public class WeChatKeyword : BaseEntity
    {
        /// <summary>
        /// 微信公众号id
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string publicAccount { get; set; }
        /// <summary>
        /// 触发关键词
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string key { get; set; }
        /// <summary>
        /// 微信媒体id
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string media_id { get; set; }
        /// <summary>
        /// 媒体类型
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string media_type { get; set; }
        /// <summary>
        /// 视频标题
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string title { get; set; }
        /// <summary>
        /// 视频文本
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string description { get; set; }
        /// <summary>
        /// 回复文字
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string media_desc { get; set; }
        /// <summary>
        /// 访问url
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string url { get; set; }
    }
}
