﻿using Blog.Core.Model.Models.RootTkey;
using SqlSugar;

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 血糖服务器
    /// </summary>
    public class NightscoutServer : BaseEntity
    {
        /// <summary>
        /// 服务器名称
        /// </summary>
        public string serverName { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string serverIp { get; set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int serverPort { get; set; }
        public string serverLoginName { get; set; }
        public string serverLoginPassword { get; set; }

        /// <summary>
        /// 当前实例IP
        /// </summary>
        public string curInstanceIp { get; set; }
        /// <summary>
        /// 当前IP序列
        /// </summary>
        public int curInstanceIpSerial { get; set; }
        /// <summary>
        /// 实例模板ip
        /// </summary>
        public string instanceIpTemplate { get; set; }

        /// <summary>
        /// 当前暴露端口(服务器IP+暴露端口)(如果为0则为实例IP+1337)
        /// </summary>
        public int curExposedPort { get; set; }
        /// <summary>
        /// 当前服务序列
        /// </summary>
        public int curServiceSerial { get; set; }


        /// <summary>
        /// MongoDB数据库
        /// </summary>
        public string mongoIp { get; set; }
        /// <summary>
        /// MongoDB端口
        /// </summary>
        public int mongoPort { get; set; }
        /// <summary>
        /// mongo登录账号
        /// </summary>

        public string mongoLoginName { get; set; }
        /// <summary>
        /// mongo登录密码
        /// </summary>

        public string mongoLoginPassword { get; set; }
        /// <summary>
        /// 备注
        /// </summary>

        public string remark { get; set; }
        /// <summary>
        /// 是否nginx节点服务器
        /// </summary>
        public bool isNginx { get; set; }
        /// <summary>
        /// 是否mongo节点服务器
        /// </summary>
        public bool isMongo { get; set; }
        /// <summary>
        /// 统计数量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int count { get; set; }

    }
}
