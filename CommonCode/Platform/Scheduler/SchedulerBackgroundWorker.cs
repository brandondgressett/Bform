using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using LinqKit;
using Microsoft.Extensions.Hosting;
using NCrontab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Scheduler
{
    [Obsolete("Use Quartz.NET scheduler via AddBFormQuartzScheduler() instead. This legacy scheduler will be removed in a future version.")]
    public class SchedulerBackgroundWorker: BackgroundService
    {

        IRepository<ScheduledJobEntity> _repo;
        
        AppEventSink _aes;


        public SchedulerBackgroundWorker(IRepository<ScheduledJobEntity> repo, AppEventSink aes)
        {
            _repo = repo; 
            
            _aes = aes;

        }


        private async Task ScheduleJobs()
        {
            var (schedules, ctx) = await _repo.GetOrderedAsync(sch => sch.NextDeadline, descending: true, start: 0, count: 1000);

            foreach (var schedule in schedules)
            {
                // if this job's deadline has passed
                if (schedule.NextDeadline < DateTime.UtcNow)
                {
                    // see if we should stop the job from here on out
                    bool scheduleDone = false;
                    switch (schedule.Payload!.Type)
                    {
                        case ScheduleType.Once: // this one runs just once and stops forever
                            scheduleDone = true;
                            break;
                        case ScheduleType.RecurringX:
                            schedule.Payload!.InvocationCount++;
                            if (schedule.Payload!.InvocationCount >= schedule.Payload!.RecurrenceCount) // happens X times then stops
                                scheduleDone = true;
                            break;
                        case ScheduleType.RecurringInfinite: // never stops
                            schedule.Payload!.InvocationCount++;
                            break;
                        case ScheduleType.Cron: // TODO: see if we should stop. Do Cron Expressions loop forever or do they stop.

                            break;
                    }

                    if (scheduleDone)
                    {
                        await _repo.DeleteAsync((schedule, ctx)); // delete the job because its done
                    }
                    else // this job doesn't stop, so schedule the next deadline
                    {
                        // TODO: if cron, use that instead
                        if (!string.IsNullOrWhiteSpace(schedule.Payload!.CronExpression))
                        {
                            var cron = CrontabSchedule.Parse(schedule.Payload!.CronExpression);
                            var next = cron.GetNextOccurrence(DateTime.UtcNow);
                            schedule.NextDeadline = next;
                        }
                        else
                        {
                            schedule.NextDeadline = DateTime.UtcNow + schedule.RecurrenceSchedule;
                            await _repo.UpdateAsync((schedule, ctx));
                        }
                    }

                    var handle = _repo.OpenTransaction();

                    _aes.BeginBatch(handle);

                    // send the event to invoke the scheduled job
                    await _aes.Enqueue(new AppEventOrigin(nameof(SchedulerBackgroundWorker), nameof(ScheduleJobs), null), 
                        schedule.Payload!.Topic!, nameof(ScheduleJobs), schedule, null, null);

                    await _aes.CommitBatch();

                }
            }
        }

        /// <summary>
        /// Check any jobs that are due.
        /// Note: when a job is created, the first deadline must be set!
        /// For any due jobs, send its event and schedule the next deadline.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var task = new Task(async () =>
            {
                while(!stoppingToken.IsCancellationRequested)
                {
                    await ScheduleJobs();
                    System.Threading.Thread.Yield();
                }
            });

            task.Start();

            return Task.CompletedTask;

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    // get any jobs that are over due
            //    var (schedules, ctx) = await _repo.GetOrderedAsync(sch => sch.NextDeadline, descending: true, start: 0, count: 1000);

            //    foreach (var schedule in schedules)
            //    {
            //        // if this job's deadline has passed
            //        if (schedule.NextDeadline < DateTime.UtcNow)
            //        {
            //            // see if we should stop the job from here on out
            //            bool scheduleDone = false;
            //            switch (schedule.Type)
            //            {
            //                case Schedule.ScheduleType.Once: // this one runs just once and stops forever
            //                    scheduleDone = true;
            //                    break;
            //                case Schedule.ScheduleType.RecurringX:
            //                    schedule.InvocationCount++;
            //                    if (schedule.InvocationCount >= schedule.RecurrenceCount) // happens X times then stops
            //                        scheduleDone = true;
            //                    break;
            //                case Schedule.ScheduleType.RecurringInfinite: // never stops
            //                    schedule.InvocationCount++;
            //                    break;
            //                case Schedule.ScheduleType.Cron: // TODO: see if we should stop. Do Cron Expressions loop forever or do they stop.
                                
            //                    break;
            //            }

            //            if (scheduleDone)
            //            {
            //                await _repo.DeleteAsync((schedule, ctx)); // delete the job because its done
            //            }
            //            else // this job doesn't stop, so schedule the next deadline
            //            {
            //                // TODO: if cron, use that instead
            //                if (!string.IsNullOrWhiteSpace(schedule.CronExpression))
            //                {
            //                    var cron = CrontabSchedule.Parse(schedule.CronExpression);
            //                    var next = cron.GetNextOccurrence(DateTime.UtcNow);
            //                    schedule.NextDeadline = next;
            //                }
            //                else
            //                {
            //                    schedule.NextDeadline = DateTime.UtcNow + schedule.RecurrenceSchedule;
            //                    await _repo.UpdateAsync((schedule, ctx));
            //                }
            //            }

            //            // send the event to invoke the scheduled job
            //            await _aes.Enqueue(new AppEventOrigin(nameof(schedule.Job), schedule.Job!.Action, new AppEvent()), schedule.Job!.Payload!.Topic!, schedule.Job.Action, schedule.Job!, null, null);//I Think making this a guarenteed not null is an issue CAG
            //        }

            //    }
                
            //    System.Threading.Thread.Yield();
            //} 

            

            
           
        }
        
       
    }
}
