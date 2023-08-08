using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model.Models;
using Microsoft.Extensions.Logging;
using Quartz;
using Renci.SshNet;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到(任务类要去JobSetup添加注入)
/// </summary>
namespace Blog.Core.Tasks
{
    public class Job_RestartDocker : JobBase, IJob
    {
        private readonly IBaseServices<NightscoutServer> _nightscoutServerServices;

        public Job_RestartDocker(ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices, IBaseServices<NightscoutServer> nightscoutServerServices)
            : base(tasksQzServices, tasksLogServices)
        {
            _tasksQzServices = tasksQzServices;
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
                //重启docker服务
                var master = (await _nightscoutServerServices.Query(t => t.isNginx == true)).FirstOrDefault();
                if (master != null)
                {
                    using (var sshMasterClient = new SshClient(master.serverIp, master.serverPort, master.serverLoginName, master.serverLoginPassword))
                    {
                        sshMasterClient.Connect();
                        using (var cmdMaster = sshMasterClient.CreateCommand(""))
                        {
                            JobDataMap data = context.JobDetail.JobDataMap;
                            string pars = data.GetString("JobParam");
                            var resMaster = cmdMaster.Execute(pars);
                        }
                        sshMasterClient.Disconnect();
                    }
                }
            }
        }
    }



}
