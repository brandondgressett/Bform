using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.DependencyInjection;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Scheduler;

/// <summary>
/// QuartzISchedulerLogic manages creating and stopping scheduled events based on triggers. 
///     -References:
///         >WorkItemLogic.cs
///     -Functions:
///         >MaybeInitialize
///         >Stop
///         >EventScheduleEvents
///         >ScheduleJob
///         >CreateTrigger
///         >Deschedule
/// </summary>
[Obsolete("Use QuartzISchedulerLogic via AddBFormQuartzScheduler() instead. This legacy scheduler will be removed in a future version.")]
public class QuartzISchedulerLogic
{

    //private readonly IBackgroundJobClient _sf;
   
    //private IRecurringJobManager _recurringJobManager;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationPlatformContent _content;
    private bool _initialized = false;
    private readonly object _door = new object();
    private readonly TemplateNamesCache _tnCache;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly IRepository<ScheduledJobEntity> _repo;
    private readonly IServiceProvider _serviceProvider;
    
    public QuartzISchedulerLogic(
        TemplateNamesCache tnCache,
        IApplicationPlatformContent content,
        IApplicationAlert alerts,
    
        IRepository<ScheduledJobEntity> repo,
        IServiceProvider sp)//,
        //IRecurringJobManager recurringJobManager)
    {
        _content = content;
        _tnCache = tnCache;
        _alerts = alerts;
      
        _repo = repo;
        _serviceProvider = sp;
        //_recurringJobManager = recurringJobManager;
        //_sf = schedulerFactory;
    }

    //private async Task StartSch()
    //{
    //    //_scheduler = await _sf.GetScheduler();
    //    //_scheduler.Guarantees().IsNotNull();
    //    //await  _scheduler.(_cts.Token);
    //}

    private void MaybeInitialize()
    {
        if (_initialized)
            return;

        lock(_door)
        {
            if (!_initialized)
            {
                //AsyncHelper.RunSync(() => StartSch() );
                _initialized = true;
            }
        }
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public const string DefaultWorkSetTemplate = "global_scheduler";
    public const string DefaultWorkItemTemplate = "global_scheduled_event";


    public async Task<ScheduledEventIdentifier> EventScheduleEvents(
        ScheduledEventTemplate template,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventScheduleEvents)))
        {
            try
            {
                MaybeInitialize();

                string wsTemplate = DefaultWorkSetTemplate,
                       wiTemplate = DefaultWorkItemTemplate;

                if (hostWorkSet is not null && hostWorkItem is not null)
                    (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(hostWorkSet.Value, hostWorkItem.Value);

                Guid scheduleId = Guid.NewGuid();
                if(!string.IsNullOrWhiteSpace(attachedItem))
                    scheduleId = Guid.Parse(attachedItem);

                var identifier = template.CreateIdentifier(scheduleId);
                var topic = template.CreateEventTopic(wsTemplate, wiTemplate);

                var scheduledInstance = new ScheduledEvent
                {
                    Content = content,
                    CreatedDate = DateTime.UtcNow,
                    Schedule = template.Schedule,
                    EventTopic = topic,
                    HostWorkItem = hostWorkItem,
                    HostWorkSet = hostWorkSet,
                    Id = Guid.NewGuid(),
                    Tags = tags.EmptyIfNull().ToList(),
                    Template = template.Name,
                    UpdatedDate = DateTime.UtcNow,
                    AttachedTo = attachedItem
                };

                var json = JsonConvert.SerializeObject(scheduledInstance);
                var jobInstance = _serviceProvider.GetService<SinkEventJob>();
                //var jobInstance = Activator.CreateInstance<SinkEventJob>();

                //var job = _scheduler?.Create(() => jobInstance.Execute(), new EnqueuedState());
                //_scheduler?.Schedule(async () => await jobInstance.Execute());
                    
                return identifier;


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation());

                throw;
            }
        }
    }

    public async Task<ScheduledEventIdentifier> EventScheduleEvents(
        string templateName,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null)
    {

        var template = _content.GetContentByName<ScheduledEventTemplate>(templateName)!;
        template.Guarantees().IsNotNull();

        return await EventScheduleEvents(template, content, hostWorkSet, hostWorkItem, attachedItem, tags); 

        
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TJob"></typeparam>
    /// <param name="template">template.schedule should be formatted like: Repeat count is formatted "6.12:14:45|5" this is 6 days 12 hours 14 minutes and 45 seconds, |5 is 5 times repeated For more info:https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0</param>
    /// <param name="jobData"></param>
    /// <returns></returns>
    public ScheduledEventIdentifier ScheduleJob<TJob>(
        ScheduledEventTemplate template,
        JObject jobData) where TJob: IJobIntegration
    {
        using (PerfTrack.Stopwatch(nameof(ScheduleJob)))
        {
            try
            {
                MaybeInitialize();
                
                var identifier = template.CreateIdentifier(Guid.NewGuid());

                var newSched = new ScheduledJobEntity() { Payload = new ScheduledJobData() };

                //Format: 6.12:14:45 is 6 days 12 hours 14 minutes and 45 seconds can just give "6" which is 6 days and formatting like a digital clock will give you times; 12:14:45. More at: https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0
                if (template.Schedule.StartsWith("ts:"))
                {
                    var schedule = template.Schedule;
                    var meat = schedule[3..];
                    var ts = TimeSpan.Parse(meat);
                    newSched.RecurrenceSchedule = ts;
                    newSched.NextDeadline = DateTime.UtcNow.Add(newSched.RecurrenceSchedule);
                    newSched.Payload.Type = ScheduleType.Once;
                }
                //Format: 6.12:14:45|5 is 6 days 12 hours 14 minutes 45 seconds, and repeated 5 times. You can just give "6" which is 6 days and formatting like a digital clock will give you times; 12:14:45 More at: https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0
                if (template.Schedule.StartsWith("rc:"))
                {
                    var schedule = template.Schedule;
                    var meat = schedule[3..];
                    var meatComponents = meat.Split("|");
                    var timeBit = meatComponents[0];
                    var count = meatComponents[1];

                    var ts = TimeSpan.Parse(timeBit);
                    newSched.RecurrenceSchedule = ts;
                    newSched.Payload.RecurrenceCount = int.Parse(count);
                    newSched.NextDeadline = DateTime.UtcNow.Add(newSched.RecurrenceSchedule);
                    newSched.Payload.Type = ScheduleType.RecurringX;
                }
                //Format: 6.12:14:45 is 6 days 12 hours 14 minutes and 45 seconds can just give "6" which is 6 days and formatting like a digital clock will give you times; 12:14:45. More at: https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-7.0
                if (template.Schedule.StartsWith("rf:"))
                {
                    var schedule = template.Schedule;
                    var meat = schedule[3..];
                    newSched.RecurrenceSchedule = TimeSpan.Parse(meat);
                    newSched.Payload.RepeatForever = true;
                    newSched.NextDeadline = DateTime.UtcNow.Add(newSched.RecurrenceSchedule);
                    newSched.Payload.Type = ScheduleType.RecurringInfinite;
                }
                //Format: "cronExpression"
                if (template.Schedule.StartsWith("cr:"))
                {
                    var schedule = template.Schedule;
                    var meat = schedule[3..];
                    newSched.Payload.CronExpression = meat;

                    var cronSched = CrontabSchedule.Parse(meat);

                    var next = cronSched.GetNextOccurrence(DateTime.UtcNow);
                    newSched.NextDeadline = next;
                    newSched.Payload.Type = ScheduleType.Cron;
                }

                newSched.Payload.JobContent = jobData;
                newSched.Payload.Name = template.EventTopic;
                newSched.Payload.ScheduledEventID = identifier;

                _repo.CreateAsync(newSched);

                return identifier;

            } catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    ex.TraceInformation());

                throw;
            }

        }
    }

    private void MayInitDeschedule(string jobID)
    {
        MaybeInitialize();

    }


    public void Deschedule(string schedule)//CAG Change all deschedules don't work at the time
    {
        MayInitDeschedule(schedule);
    }

    public void Deschedule(List<string> schedules)//CAG Change
    {
        foreach(var schedule in schedules)
            MayInitDeschedule(schedule);
    }


    

}
