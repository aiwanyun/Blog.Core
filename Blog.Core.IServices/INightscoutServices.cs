using Blog.Core.Common.Helper;
using Blog.Core.Common;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

namespace Blog.Core.IServices
{	
	/// <summary>
	/// InightscoutServices
	/// </summary>	
    public interface INightscoutServices :IBaseServices<Nightscout>
	{
        /// <summary>
        /// 启动/删除实例
        /// </summary>
        /// <param name="nightscout"></param>
        /// <param name="nsserver"></param>
        /// <returns></returns>
        public Task RunDocker(Nightscout nightscout, NightscoutServer nsserver);


        /// <summary>
        /// 停止实例
        /// </summary>
        /// <param name="nightscout"></param>
        /// <param name="nsserver"></param>
        /// <returns></returns>
        public Task StopDocker(Nightscout nightscout, NightscoutServer nsserver);
        /// <summary>
        /// 删除实例(会清理数据)
        /// </summary>
        /// <param name="nightscout"></param>
        /// <param name="nsserver"></param>
        /// <returns></returns>
        public Task DeleteData(Nightscout nightscout, NightscoutServer nsserver);
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="nightscout"></param>
        /// <param name="nsserver"></param>
        /// <returns></returns>
        public Task InitData(Nightscout nightscout, NightscoutServer nsserver);
        /// <summary>
        /// 添加国内解析
        /// </summary>
        /// <param name="nightscout"></param>
        /// <returns></returns>

        public Task<bool> ResolveDomain(Nightscout nightscout);
        /// <summary>
        /// 删除国内解析
        /// </summary>
        /// <param name="nightscout"></param>
        /// <returns></returns>
        public Task<bool> UnResolveDomain(Nightscout nightscout);
    }
}

