using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl.Matchers;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Quartz.NET implementation of the scheduler logic.
/// Replaces the custom polling-based scheduler with Quartz.
/// </summary>
public class QuartzSchedulerLogic : QuartzISchedulerLogic
{
    private readonly IScheduler _scheduler;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationPlatformContent _content;
    private readonly TemplateNamesCache _tnCache;
    private readonly ILogger<QuartzSchedulerLogic> _logger;
    private readonly ITenantContext _tenantContext;

    public const string DefaultWorkSetTemplate = "global_scheduler";
    public const string DefaultWorkItemTemplate = "global_scheduled_event";

    public QuartzSchedulerLogic(
        IScheduler scheduler,
        TemplateNamesCache tnCache,
        IApplicationPlatformContent content,
        IApplicationAlert alerts,
        ILogger<QuartzSchedulerLogic> logger,
        ITenantContext tenantContext)
    {
        _scheduler = scheduler;
        _content = content;
        _tnCache = tnCache;
        _alerts = alerts;
        _logger = logger;
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ScheduledEventIdentifier> EventScheduleEventsAsync(
        ScheduledEventTemplate template,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        using (PerfTrack.Stopwatch(nameof(EventScheduleEventsAsync)))
        {
            try
            {
                string wsTemplate = DefaultWorkSetTemplate,
                       wiTemplate = DefaultWorkItemTemplate;

                if (hostWorkSet.HasValue && hostWorkItem.HasValue)
                {
                    (wsTemplate, wiTemplate) = await _tnCache.GetTemplateNames(
                        hostWorkSet.Value, hostWorkItem.Value);
                }

                Guid scheduleId = Guid.NewGuid();
                if (!string.IsNullOrWhiteSpace(attachedItem))
                {
                    scheduleId = Guid.Parse(attachedItem);
                }

                var identifier = template.CreateIdentifier(scheduleId);
                var topic = template.CreateEventTopic(wsTemplate, wiTemplate);

                // Create the scheduled event instance with tenant context
                var scheduledInstance = new ScheduledEvent
                {
                    TenantId = _tenantContext.TenantId, // Assign current tenant ID
                    Content = content,
                    CreatedDate = DateTime.UtcNow,
                    Schedule = template.Schedule,
                    EventTopic = topic,
                    HostWorkItem = hostWorkItem,
                    HostWorkSet = hostWorkSet,
                    Id = scheduleId,
                    Tags = tags?.ToList() ?? new List<string>(),
                    Template = template.Name,
                    UpdatedDate = DateTime.UtcNow,
                    AttachedTo = attachedItem
                };

                // Create Quartz job with tenant context
                var jobKey = new JobKey(scheduleId.ToString(), template.Name);
                var job = JobBuilder.Create<QuartzSinkEventJob>()
                    .WithIdentity(jobKey)
                    .WithDescription($"Event: {topic} (Tenant: {_tenantContext.TenantId ?? "Global"})")
                    .UsingJobData("eventData", JsonConvert.SerializeObject(scheduledInstance))
                    .UsingJobData("templateName", template.Name)
                    .UsingJobData("tenantId", _tenantContext.TenantId ?? "") // Add explicit tenant ID
                    .UsingJobData("hostWorkSet", hostWorkSet?.ToString() ?? "")
                    .UsingJobData("hostWorkItem", hostWorkItem?.ToString() ?? "")
                    .UsingJobData("attachedItem", attachedItem ?? "")
                    .StoreDurably(false)
                    .Build();

                // Create trigger based on schedule type
                var trigger = CreateTrigger(template, scheduleId, jobKey);

                // Schedule the job
                await _scheduler.ScheduleJob(job, trigger, cancellationToken);

                _logger.LogInformation(
                    "Scheduled event {EventId} for topic {Topic} with template {Template}",
                    scheduleId, topic, template.Name);

                return identifier;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(
                    ApplicationAlertKind.General,
                    LogLevel.Error,
                    $"Failed to schedule event: {ex.Message}");

                throw;
            }
        }
    }

    public async Task<ScheduledEventIdentifier> EventScheduleEventsAsync(
        string templateName,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var template = _content.GetContentByName<ScheduledEventTemplate>(templateName);
        template.Guarantees().IsNotNull();

        return await EventScheduleEventsAsync(
            template!, content, hostWorkSet, hostWorkItem, attachedItem, tags, cancellationToken);
    }

    public async Task<ScheduledEventIdentifier> ScheduleJobAsync<TJob>(
        ScheduledEventTemplate template,
        JObject jobData,
        CancellationToken cancellationToken = default) 
        where TJob : IJobIntegration
    {
        using (PerfTrack.Stopwatch(nameof(ScheduleJobAsync)))
        {
            try
            {
                var scheduleId = Guid.NewGuid();
                var identifier = template.CreateIdentifier(scheduleId);

                // Create job detail using the wrapper
                var jobDetail = QuartzJobIntegrationWrapperFactory.CreateJobDetail<TJob>(
                    jobName: scheduleId.ToString(),
                    groupName: template.Group ?? "IJobIntegration",
                    jobData: new JobDataMap((IDictionary<string, object>)(jobData.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>())),
                    description: template.Name);

                // Create trigger based on schedule
                var trigger = await CreateTriggerFromSchedule(
                    scheduleId.ToString(),
                    template.Group ?? "IJobIntegration",
                    template.Schedule,
                    template.EndAfter,
                    cancellationToken);

                // Schedule the job
                await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);

                _logger.LogInformation(
                    "Scheduled IJobIntegration job {JobType} with ID {ScheduleId} and schedule {Schedule}",
                    typeof(TJob).Name,
                    scheduleId,
                    template.Schedule);

                return identifier;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(
                    ApplicationAlertKind.General,
                    LogLevel.Error,
                    $"Failed to schedule IJobIntegration job: {ex.Message}");

                _logger.LogError(ex, "Error scheduling IJobIntegration job {JobType}", typeof(TJob).Name);
                throw;
            }
        }
    }

    public async Task<bool> DescheduleAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find the job in any group
            var allJobKeys = await _scheduler.GetJobKeys(
                GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
            
            var jobKey = allJobKeys.FirstOrDefault(k => k.Name == scheduleId);
            if (jobKey == null)
            {
                _logger.LogWarning("Job {ScheduleId} not found for deletion", scheduleId);
                return false;
            }

            var deleted = await _scheduler.DeleteJob(jobKey, cancellationToken);
            
            if (deleted)
            {
                _logger.LogInformation("Successfully deleted job {ScheduleId}", scheduleId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<int> DescheduleAsync(
        IEnumerable<string> scheduleIds, 
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var scheduleId in scheduleIds)
        {
            if (await DescheduleAsync(scheduleId, cancellationToken))
            {
                count++;
            }
        }
        return count;
    }

    public async Task PauseJobAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var jobKey = await FindJobKey(scheduleId, cancellationToken);
        if (jobKey != null)
        {
            await _scheduler.PauseJob(jobKey, cancellationToken);
            _logger.LogInformation("Paused job {ScheduleId}", scheduleId);
        }
    }

    public async Task ResumeJobAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var jobKey = await FindJobKey(scheduleId, cancellationToken);
        if (jobKey != null)
        {
            await _scheduler.ResumeJob(jobKey, cancellationToken);
            _logger.LogInformation("Resumed job {ScheduleId}", scheduleId);
        }
    }

    public async Task<ScheduledJobInfo?> GetJobInfoAsync(
        string scheduleId, 
        CancellationToken cancellationToken = default)
    {
        var jobKey = await FindJobKey(scheduleId, cancellationToken);
        if (jobKey == null) return null;

        var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken);
        if (jobDetail == null) return null;

        var triggers = await _scheduler.GetTriggersOfJob(jobKey, cancellationToken);
        var trigger = triggers.FirstOrDefault();

        var info = new ScheduledJobInfo
        {
            ScheduleId = scheduleId,
            JobName = jobKey.Name,
            GroupName = jobKey.Group,
            Description = jobDetail.Description,
            CreatedTime = DateTime.UtcNow, // Would need to store this in JobDataMap
            JobData = jobDetail.JobDataMap.ToDictionary(k => k.Key, v => v.Value)
        };

        if (trigger != null)
        {
            info.NextFireTime = trigger.GetNextFireTimeUtc()?.UtcDateTime;
            info.PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.UtcDateTime;
            
            var state = await _scheduler.GetTriggerState(trigger.Key, cancellationToken);
            info.Status = state switch
            {
                TriggerState.Normal => JobStatus.Scheduled,
                TriggerState.Paused => JobStatus.Paused,
                TriggerState.Complete => JobStatus.Completed,
                TriggerState.Error => JobStatus.Error,
                TriggerState.Blocked => JobStatus.Running,
                _ => JobStatus.Scheduled
            };

            // Get trigger type info
            if (trigger is ICronTrigger cronTrigger)
            {
                info.TriggerType = "Cron";
                info.CronExpression = cronTrigger.CronExpressionString;
            }
            else if (trigger is ISimpleTrigger simpleTrigger)
            {
                info.TriggerType = "Simple";
                info.RepeatInterval = simpleTrigger.RepeatInterval;
                info.RepeatCount = simpleTrigger.RepeatCount;
            }
        }

        return info;
    }

    public async Task<IEnumerable<ScheduledJobInfo>> ListJobsAsync(
        string? groupName = null,
        bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        var matcher = string.IsNullOrEmpty(groupName) 
            ? GroupMatcher<JobKey>.AnyGroup() 
            : GroupMatcher<JobKey>.GroupEquals(groupName);

        var jobKeys = await _scheduler.GetJobKeys(matcher, cancellationToken);
        var jobInfos = new List<ScheduledJobInfo>();

        foreach (var jobKey in jobKeys)
        {
            var info = await GetJobInfoAsync(jobKey.Name, cancellationToken);
            if (info != null)
            {
                if (includeCompleted || info.Status != JobStatus.Completed)
                {
                    jobInfos.Add(info);
                }
            }
        }

        return jobInfos;
    }

    private async Task<ITrigger> CreateTriggerFromSchedule(
        string triggerName,
        string groupName,
        string schedule,
        DateTime? endAfter,
        CancellationToken cancellationToken = default)
    {
        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity(triggerName, groupName);

        if (endAfter.HasValue)
        {
            triggerBuilder.EndAt(endAfter.Value);
        }

        // Parse schedule string
        if (schedule.StartsWith("ts:"))
        {
            // One-time schedule with timespan delay
            var timespan = TimeSpan.Parse(schedule[3..]);
            triggerBuilder.StartAt(DateTimeOffset.UtcNow.Add(timespan));
        }
        else if (schedule.StartsWith("rf:"))
        {
            // Repeat forever with interval
            var timespan = TimeSpan.Parse(schedule[3..]);
            triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(timespan)
                    .RepeatForever());
        }
        else if (schedule.StartsWith("rc:"))
        {
            // Repeat count times with interval
            var parts = schedule[3..].Split('|');
            var timespan = TimeSpan.Parse(parts[0]);
            var count = int.Parse(parts[1]);
            
            triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(timespan)
                    .WithRepeatCount(count));
        }
        else if (schedule.StartsWith("cr:"))
        {
            // Cron expression
            var cronExpression = schedule[3..];
            triggerBuilder
                .StartNow()
                .WithCronSchedule(cronExpression);
        }
        else
        {
            throw new ArgumentException($"Unknown schedule format: {schedule}");
        }

        return triggerBuilder.Build();
    }

    public async Task<bool> TriggerJobNowAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var jobKey = await FindJobKey(scheduleId, cancellationToken);
        if (jobKey == null) return false;

        await _scheduler.TriggerJob(jobKey, cancellationToken);
        _logger.LogInformation("Manually triggered job {ScheduleId}", scheduleId);
        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_scheduler.IsShutdown)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
        }
    }

    #region Private Helper Methods

    private ITrigger CreateTrigger(ScheduledEventTemplate template, Guid scheduleId, JobKey jobKey)
    {
        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity($"trigger_{scheduleId}", template.Name)
            .ForJob(jobKey)
            .WithDescription($"Trigger for {template.EventTopic}");

        // Parse schedule format
        if (template.Schedule.StartsWith("ts:")) // One-time delay
        {
            var timespan = TimeSpan.Parse(template.Schedule[3..]);
            triggerBuilder
                .StartAt(DateTimeOffset.UtcNow.Add(timespan))
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow());
        }
        else if (template.Schedule.StartsWith("rc:")) // Recurring with count
        {
            var parts = template.Schedule[3..].Split('|');
            var interval = TimeSpan.Parse(parts[0]);
            var count = int.Parse(parts[1]);

            triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(interval)
                    .WithRepeatCount(count - 1)
                    .WithMisfireHandlingInstructionFireNow());
        }
        else if (template.Schedule.StartsWith("rf:")) // Recurring forever
        {
            var interval = TimeSpan.Parse(template.Schedule[3..]);
            triggerBuilder
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(interval)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionFireNow());
        }
        else if (template.Schedule.StartsWith("cr:")) // Cron
        {
            var cronExpression = template.Schedule[3..];
            triggerBuilder
                .StartNow()
                .WithCronSchedule(cronExpression, x => x
                    .WithMisfireHandlingInstructionFireAndProceed());
        }
        else
        {
            throw new ArgumentException($"Unknown schedule format: {template.Schedule}");
        }

        return triggerBuilder.Build();
    }

    private async Task<JobKey?> FindJobKey(string scheduleId, CancellationToken cancellationToken)
    {
        var allJobKeys = await _scheduler.GetJobKeys(
            GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        
        return allJobKeys.FirstOrDefault(k => k.Name == scheduleId);
    }

    #endregion
}