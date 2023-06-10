using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using Blog.Core.Model.ViewModels;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到(任务类要去JobSetup添加注入)
/// </summary>
namespace Blog.Core.Tasks
{
    public class Job_Nightscout_Refresh_Quartz : JobBase, IJob
    {
        private readonly IBaseServices<NightscoutLog> _nightscoutLogServices;
        private readonly INightscoutServices _nightscoutServices;
        private readonly IWeChatConfigServices _weChatConfigServices;

        public Job_Nightscout_Refresh_Quartz(ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices
            , INightscoutServices nightscoutServices
            , IBaseServices<NightscoutLog> nightscoutLogServices
            , IWeChatConfigServices weChatConfigServices)
            : base(tasksQzServices, tasksLogServices)
        {
            _nightscoutServices = nightscoutServices;
            _nightscoutLogServices = nightscoutLogServices;
            _weChatConfigServices = weChatConfigServices;
        }
        public async Task Execute(IJobExecutionContext context)
        { 
            // 可以直接获取 JobDetail 的值
            var jobKey = context.JobDetail.Key;
            var jobId = jobKey.Name; 
            var executeLog = await ExecuteJob(context, async () => await Run(context, jobId.ObjToLong()));

        }
        public async Task Run(IJobExecutionContext context, long jobid)
        { 
            if (jobid > 0)
            {
                JobDataMap data = context.JobDetail.JobDataMap;
                string pars = data.GetString("JobParam"); 

                var nights = await _nightscoutServices.Query();
                foreach (var nightscout in nights)
                {
                    await _nightscoutServices.RunDocker(nightscout);
                }
            }
        }
    } 


}
