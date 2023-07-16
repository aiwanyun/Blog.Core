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
        /// <param name="isDelete"></param>
        /// <returns></returns>
        public Task RunDocker(Nightscout nightscout, bool isDelete = false);


        /// <summary>
        /// 停止实例
        /// </summary>
        /// <param name="nightscout"></param>
        /// <param name="isDelete"></param>
        /// <returns></returns>
        public Task StopDocker(Nightscout nightscout);
    }
}

