using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using Blog.Core.Model.ViewModels;
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
    public class Job_Nightscout_Quartz : JobBase, IJob
    {
        private readonly IBaseServices<NightscoutLog> _nightscoutLogServices;
        private readonly INightscoutServices _nightscoutServices;
        private readonly IWeChatConfigServices _weChatConfigServices;

        public Job_Nightscout_Quartz(ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices
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
                var nsConfig = JsonConvert.DeserializeObject<NightscoutRemindConfig>(pars);

                var pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
                var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
                var pushTemplateID = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Alert" }).ObjToString();
                var frontPage = AppSettings.app(new string[] { "nightscout", "FrontPage" }).ObjToString();
                var nights = await _nightscoutServices.Query(t=>t.status.Equals("已付费"));

                List<string> ls = new List<string>();
                foreach (var nightscout in nights)
                {
                    try
                    {
                        WeChatCardMsgDataDto pushData = null;
                        if (DateTime.Now.Date.AddDays(nsConfig.preDays) >= nightscout.endTime.AddDays(nsConfig.afterDays))
                        {
                            ls.Add(nightscout.name);
                            pushData = new WeChatCardMsgDataDto();
                            pushData.cardMsg = new WeChatCardMsgDetailDto();
                            pushData.cardMsg.first = $"{nightscout.name},你的ns服务即将到期";

                        }
                        else if (DateTime.Now.Date >= nightscout.endTime)
                        {
                            ls.Add(nightscout.name);
                            pushData = new WeChatCardMsgDataDto();
                            pushData.cardMsg = new WeChatCardMsgDetailDto();
                            pushData.cardMsg.first = $"{nightscout.name},你的ns服务已到期";

                        }

                        if (pushData != null)
                        {
                            pushData.cardMsg.keyword1 = $"NS服务即将到期,请尽快续费额,以免中断服务";
                            pushData.cardMsg.keyword2 = $"{nightscout.endTime.ToString("yyyy-MM-dd HH:mm:ss")}";
                            pushData.cardMsg.remark = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            pushData.cardMsg.url = $"https://{nightscout.url}";
                            pushData.cardMsg.template_id = pushTemplateID;
                            pushData.info = new WeChatUserInfo();
                            pushData.info.id = pushWechatID;
                            pushData.info.companyCode = pushCompanyCode;
                            pushData.info.userID = nightscout.Id.ToString();
                            await _weChatConfigServices.PushCardMsg(pushData, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"{nightscout.name}推送失败,{ex.Message}");
                        LogHelper.Error(ex.StackTrace);
                    }
                }
                if (ls.Count > 0)
                {
                    try
                    {
                        var pushUsers = nsConfig.pushUserIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);
                        if (pushUsers.Length > 0)
                        {
                            foreach (var userid in pushUsers)
                            {
                                var pushData = new WeChatCardMsgDataDto();
                                pushData.cardMsg = new WeChatCardMsgDetailDto();
                                pushData.cardMsg.keyword1 = $"有{ls.Count}个客户即将到期或已到期";
                                pushData.cardMsg.keyword2 = string.Join(",", ls);
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
    /// <summary>
    /// Nightscout定期检测提醒配置
    /// </summary>
    public class NightscoutRemindConfig
    {
        public string pushWechatID { get; set; }
        public string pushCompanyCode { get; set; }
        public string pushTemplateID { get; set; }
        public string pushUserIDs { get; set; }
        public int preDays { get; set; }
        public int afterDays { get; set; }
    }



}
