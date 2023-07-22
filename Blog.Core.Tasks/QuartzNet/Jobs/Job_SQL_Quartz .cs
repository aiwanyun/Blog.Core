using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到(任务类要去JobSetup添加注入)
/// </summary>
namespace Blog.Core.Tasks
{
    public class Job_SQL_Quartz : JobBase, IJob
    {

        public Job_SQL_Quartz(ITasksQzServices tasksQzServices, ITasksLogServices tasksLogServices)
            : base(tasksQzServices, tasksLogServices)
        {
            _tasksQzServices = tasksQzServices;
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
                await _tasksQzServices.Db.Ado.ExecuteCommandAsync(pars);
            }
        }
    }



}
