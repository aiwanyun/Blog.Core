﻿using System;
using Blog.Core.Model.Models.RootTkey;
using SqlSugar;

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 血糖管理实例
    /// </summary>
    public class Nightscout: BaseEntity
    {
        /// <summary>
        /// 名称
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string name { get; set; }
        /// <summary>
        /// 访问地址
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string url { get; set; }
        /// <summary>
        /// 电话
        /// </summary>
        [SugarColumn(Length = 20, IsNullable = true)]
        public string tel { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string passwd { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string remark { get; set; }
        /// <summary>
        /// 访问地址
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string backupurl { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime startTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime endTime { get; set; }
        /// <summary>
        /// 是否刷新
        /// </summary>
        public bool isRefresh { get; set; }
        /// <summary>
        /// 是否接入
        /// </summary>
        public bool isConnection { get; set; }

        /// <summary>
        /// 是否持续推送血糖(要开启isConnection)
        /// </summary>
        public bool isKeepPush { get; set; }
        /// <summary>
        /// 服务名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string serviceName { get; set; }
        /// <summary>
        /// 实例ID
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string instanceIP { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string status { get; set; }
        /// <summary>
        /// 来源
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string resource { get; set; }
        /// <summary>
        /// 是否绑定公众号
        /// </summary>
        [SugarColumn(IsIgnore = true)]

        public bool isBindWeChat { get; set; }
        /// <summary>
        /// 是否绑定小程序
        /// </summary>
        [SugarColumn(IsIgnore = true)]

        public bool isBindMini { get; set; }
        /// <summary>
        /// 启用插件
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string plugins { get; set; }
        /// <summary>
        /// 关联服务器id
        /// </summary>
        public long serverId { get; set; }
        /// <summary>
        /// 暴露外网端口
        /// </summary>
        public int exposedPort { get; set; }
        /// <summary>
        /// 当前服务序列
        /// </summary>
        public int serviceSerial { get; set; }
        /// <summary>
        /// 是否主动停止
        /// </summary>
        public bool isStop { get; set; }
        /// <summary>
        /// 是否国内解析
        /// </summary>
        public bool isChina { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        public double money { get; set; }






    }
}
