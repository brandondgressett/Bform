using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Represents a historical record of job execution.
/// </summary>
public class JobExecutionHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// The unique identifier of the scheduled job.
    /// </summary>
    public string ScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the job.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// The group the job belongs to.
    /// </summary>
    public string JobGroup { get; set; } = string.Empty;

    /// <summary>
    /// The type of job executed.
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// When the job execution started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the job execution completed.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration of the job execution.
    /// </summary>
    [BsonIgnoreIfNull]
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// The status of the job execution.
    /// </summary>
    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Running;

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if the job failed.
    /// </summary>
    [BsonIgnoreIfNull]
    public string? StackTrace { get; set; }

    /// <summary>
    /// The trigger that caused this execution.
    /// </summary>
    public string TriggerName { get; set; } = string.Empty;

    /// <summary>
    /// The scheduled fire time of the trigger.
    /// </summary>
    public DateTime ScheduledFireTime { get; set; }

    /// <summary>
    /// The actual fire time of the trigger.
    /// </summary>
    public DateTime ActualFireTime { get; set; }

    /// <summary>
    /// Job data map that was passed to the job.
    /// </summary>
    [BsonIgnoreIfNull]
    public Dictionary<string, object>? JobData { get; set; }

    /// <summary>
    /// The host/instance that executed the job.
    /// </summary>
    public string ExecutingHost { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking related operations.
    /// </summary>
    [BsonIgnoreIfNull]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Number of retry attempts if the job failed and was retried.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Whether this was a recovery execution after a misfire.
    /// </summary>
    public bool IsRecovery { get; set; }

    /// <summary>
    /// Custom metadata about the execution.
    /// </summary>
    [BsonIgnoreIfNull]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// When this history record expires and can be deleted.
    /// </summary>
    [BsonIgnoreIfNull]
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Status of a job execution.
/// </summary>
public enum JobExecutionStatus
{
    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was vetoed by a trigger listener.
    /// </summary>
    Vetoed,

    /// <summary>
    /// Job execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Job timed out during execution.
    /// </summary>
    Timeout
}