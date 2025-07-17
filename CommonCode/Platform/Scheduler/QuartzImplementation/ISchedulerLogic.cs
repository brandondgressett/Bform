using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Interface for scheduler logic implementations.
/// This allows swapping between the custom scheduler and Quartz implementation.
/// </summary>
public interface QuartzISchedulerLogic
{
    /// <summary>
    /// Schedules an event-based job using a template.
    /// </summary>
    Task<ScheduledEventIdentifier> EventScheduleEventsAsync(
        ScheduledEventTemplate template,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules an event-based job using a template name.
    /// </summary>
    Task<ScheduledEventIdentifier> EventScheduleEventsAsync(
        string templateName,
        JObject content,
        Guid? hostWorkSet,
        Guid? hostWorkItem,
        string? attachedItem,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a job that implements IJobIntegration.
    /// </summary>
    Task<ScheduledEventIdentifier> ScheduleJobAsync<TJob>(
        ScheduledEventTemplate template,
        JObject jobData,
        CancellationToken cancellationToken = default) 
        where TJob : IJobIntegration;

    /// <summary>
    /// Removes a scheduled job.
    /// </summary>
    Task<bool> DescheduleAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple scheduled jobs.
    /// </summary>
    Task<int> DescheduleAsync(IEnumerable<string> scheduleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a scheduled job.
    /// </summary>
    Task PauseJobAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused job.
    /// </summary>
    Task ResumeJobAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a scheduled job.
    /// </summary>
    Task<ScheduledJobInfo?> GetJobInfoAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all scheduled jobs, optionally filtered by criteria.
    /// </summary>
    Task<IEnumerable<ScheduledJobInfo>> ListJobsAsync(
        string? groupName = null,
        bool includeCompleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a job to run immediately, regardless of its schedule.
    /// </summary>
    Task<bool> TriggerJobNowAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the scheduler gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a scheduled job.
/// </summary>
public class ScheduledJobInfo
{
    public string ScheduleId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public JobStatus Status { get; set; }
    public string? TriggerType { get; set; }
    public string? CronExpression { get; set; }
    public TimeSpan? RepeatInterval { get; set; }
    public int? RepeatCount { get; set; }
    public int ExecutionCount { get; set; }
    public Dictionary<string, object> JobData { get; set; } = new();
}

/// <summary>
/// Status of a scheduled job.
/// </summary>
public enum JobStatus
{
    Scheduled,
    Running,
    Paused,
    Completed,
    Error,
    Deleted
}