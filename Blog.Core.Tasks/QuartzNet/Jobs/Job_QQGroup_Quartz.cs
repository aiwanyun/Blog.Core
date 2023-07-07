
using Blog.Core.Common;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using Blog.Core.Repository.UnitOfWorks;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到(任务类要去JobSetup添加注入)
/// </summary>
namespace Blog.Core.Tasks
{
    public class Job_QQGroup_Quartz : JobBase, IJob
    {
        public IBaseServices<TrojanDetails> _DetailServices;

        public Job_QQGroup_Quartz( ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices)
            : base(tasksQzServices, tasksLogServices)
        {
            _tasksQzServices = tasksQzServices;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            //var param = context.MergedJobDataMap;
            // 可以直接获取 JobDetail 的值
            var jobKey = context.JobDetail.Key;
            var jobId = jobKey.Name;
            var executeLog = await ExecuteJob(context, async () => await Run(context, jobId.ObjToLong()));

        }
        public async Task Run(IJobExecutionContext context, long jobid)
        {
            if (jobid > 0)
            {
                try
                {
                    var sendMsg = $"[CQ:at,qq=all] \n 28分小游戏,开搞啦开搞啦.北京时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                    var groupid = 894466423;
                    var token = AppSettings.app(new string[] { "qqGroupPushToken" }).ObjToString();
                    var sendUrl = AppSettings.app(new string[] { "qqGroupPushUrl" }).ObjToString();

                    
                    string requestJson;
                    GrouInfo sendObj = new GrouInfo();
                    sendObj.auto_escape = false;
                    var tempMsg = sendMsg;
                    sendObj.message = tempMsg;
                    sendObj.group_id = groupid;
                    requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(sendObj);

                    string result = string.Empty;
                    using (HttpContent httpContent = new StringContent(requestJson))
                    {
                        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("Authorization", token);
                        //httpClient.Timeout = TimeSpan.FromSeconds(60);
                        result = await httpClient.PostAsync(sendUrl + "/send_group_msg", httpContent).Result.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }

    public class GrouInfo
    {
        public int group_id { get; set; }
        public string message { get; set; }
        public bool auto_escape { get; set; } = false;
    }

}
