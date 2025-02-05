﻿using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using Blog.Core.Model.ViewModels;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using Serilog;
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
        private readonly IBaseServices<NightscoutServer> _nightscoutServerServices;

        public Job_Nightscout_Refresh_Quartz(ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices
            , INightscoutServices nightscoutServices
            , IBaseServices<NightscoutLog> nightscoutLogServices
            , IBaseServices<NightscoutServer> nightscoutServerServices
            , IWeChatConfigServices weChatConfigServices)
            : base(tasksQzServices, tasksLogServices)
        {
            _nightscoutServices = nightscoutServices;
            _nightscoutLogServices = nightscoutLogServices;
            _weChatConfigServices = weChatConfigServices;
            _nightscoutServerServices = nightscoutServerServices;
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
                var nsConfig = JsonConvert.DeserializeObject<NightscoutRemindConfig>(pars);

                var nights = await _nightscoutServices.Query();
                List<string> errCount = new List<string>();
                foreach (var nightscout in nights)
                {
                    try
                    {
                        if (nightscout.isStop) continue;
                        var nsserver = await _nightscoutServerServices.QueryById(nightscout.serverId);
                        await _nightscoutServices.StopDocker(nightscout, nsserver);
                        await _nightscoutServices.RunDocker(nightscout, nsserver);
                    }
                    catch (Exception ex)
                    {
                        errCount.Add(nightscout.name);
                        LogHelper.Error($"{nightscout.name}-重启实例失败:{ex.Message}");
                    }
                }
                if (errCount.Count > 0)
                {
                    try
                    {
                        var pushUsers = nsConfig.pushUserIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);
                        if (pushUsers.Length > 0)
                        {
                            var pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
                            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
                            var pushTemplateID = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Alert" }).ObjToString();
                            var frontPage = AppSettings.app(new string[] { "nightscout", "FrontPage" }).ObjToString();
                            foreach (var userid in pushUsers)
                            {
                                var pushData = new WeChatCardMsgDataDto();
                                pushData.cardMsg = new WeChatCardMsgDetailDto();
                                pushData.cardMsg.keyword1 = $"每周ns重启任务出现失败:{errCount.Count}个";
                                pushData.cardMsg.keyword2 = string.Join(",", errCount);
                                pushData.cardMsg.remark = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                pushData.cardMsg.url = frontPage;
                                pushData.cardMsg.template_id = pushTemplateID;
                                pushData.info = new WeChatUserInfo();
                                pushData.info.id = pushWechatID;
                                pushData.info.companyCode = pushCompanyCode;
                                pushData.info.userID = userid;
                                await _weChatConfigServices.PushCardMsg(pushData, "");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"推送失败,{ex.Message}");
                        LogHelper.Error(ex.StackTrace);
                    }
                }
            }
        }
    } 


}
