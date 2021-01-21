using Quartz;
using Quartz.Impl;

namespace OPC.Task.Task
{
    public class TaskMain
    {
        readonly StdSchedulerFactory _factory = new StdSchedulerFactory();
        IScheduler _scheduler;
        public async System.Threading.Tasks.Task Start()
        {
            _scheduler = await _factory.GetScheduler();
            await _scheduler.Start();
            IJobDetail job = JobBuilder.Create<OPCTask>().Build();
            ITrigger trigger = TriggerBuilder.Create().WithIdentity("opcTrigger", "opcGroup").WithSimpleSchedule(
                x => x.WithIntervalInSeconds(60).RepeatForever()).Build();
            await _scheduler.ScheduleJob(job, trigger);
        }

        public async System.Threading.Tasks.Task Stop()
        {
            if (_scheduler != null)
            {
                await _scheduler.Shutdown();
            }
        }
    }
}