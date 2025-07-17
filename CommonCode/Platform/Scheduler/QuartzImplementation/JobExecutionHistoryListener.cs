using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Quartz;
using Quartz.Listener;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Quartz job listener that tracks job execution history in MongoDB.
/// </summary>
public class JobExecutionHistoryListener : JobListenerSupport
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobExecutionHistoryListener> _logger;
    private readonly JobHistoryOptions _options;
    private readonly string _schedulerInstanceId;

    public override string Name => "JobExecutionHistoryListener";

    public JobExecutionHistoryListener(
        IServiceProvider serviceProvider,
        ILogger<JobExecutionHistoryListener> logger,
        JobHistoryOptions options,
        string schedulerInstanceId)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _schedulerInstanceId = schedulerInstanceId;
    }

    public override async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            var collection = database.GetCollection<JobExecutionHistory>(_options.CollectionName);

            var history = new JobExecutionHistory
            {
                ScheduleId = context.JobDetail.Key.Name,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobType = context.JobDetail.JobType.FullName ?? "Unknown",
                StartTime = DateTime.UtcNow,
                Status = JobExecutionStatus.Running,
                TriggerName = context.Trigger.Key.ToString(),
                ScheduledFireTime = context.ScheduledFireTimeUtc?.UtcDateTime ?? DateTime.UtcNow,
                ActualFireTime = context.FireTimeUtc.UtcDateTime,
                ExecutingHost = _schedulerInstanceId,
                IsRecovery = context.Recovering,
                RetryCount = context.RefireCount,
                JobData = context.MergedJobDataMap?.ToDictionary(k => k.Key, v => v.Value)
            };

            // Set expiration if retention is configured
            if (_options.RetentionDays > 0)
            {
                history.ExpiresAt = DateTime.UtcNow.AddDays(_options.RetentionDays);
            }

            // Add correlation ID if available
            if (context.MergedJobDataMap?.ContainsKey("CorrelationId") == true)
            {
                history.CorrelationId = context.MergedJobDataMap["CorrelationId"]?.ToString();
            }

            await collection.InsertOneAsync(history, cancellationToken: cancellationToken);

            // Store the history ID in the context for later update
            context.Put("HistoryId", history.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record job execution start for {JobKey}", context.JobDetail.Key);
            // Don't throw - we don't want to prevent job execution due to history tracking failure
        }
    }

    public override async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        try
        {
            var historyId = context.Get("HistoryId") as string;
            if (string.IsNullOrEmpty(historyId)) return;

            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            var collection = database.GetCollection<JobExecutionHistory>(_options.CollectionName);

            var endTime = DateTime.UtcNow;
            var status = jobException == null ? JobExecutionStatus.Succeeded : JobExecutionStatus.Failed;

            var update = Builders<JobExecutionHistory>.Update
                .Set(h => h.EndTime, endTime)
                .Set(h => h.Status, status);

            if (jobException != null)
            {
                update = update
                    .Set(h => h.ErrorMessage, jobException.Message)
                    .Set(h => h.StackTrace, jobException.ToString());
            }

            // Add any custom metadata
            if (_options.MetadataProvider != null)
            {
                var metadata = await _options.MetadataProvider(context);
                if (metadata != null && metadata.Any())
                {
                    update = update.Set(h => h.Metadata, metadata);
                }
            }

            await collection.UpdateOneAsync(
                h => h.Id == historyId,
                update,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Recorded job execution completion for {JobKey} with status {Status}",
                context.JobDetail.Key,
                status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record job execution completion for {JobKey}", context.JobDetail.Key);
        }
    }

    public override async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            var collection = database.GetCollection<JobExecutionHistory>(_options.CollectionName);

            var history = new JobExecutionHistory
            {
                ScheduleId = context.JobDetail.Key.Name,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobType = context.JobDetail.JobType.FullName ?? "Unknown",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Status = JobExecutionStatus.Vetoed,
                TriggerName = context.Trigger.Key.ToString(),
                ScheduledFireTime = context.ScheduledFireTimeUtc?.UtcDateTime ?? DateTime.UtcNow,
                ActualFireTime = context.FireTimeUtc.UtcDateTime,
                ExecutingHost = _schedulerInstanceId,
                IsRecovery = context.Recovering
            };

            if (_options.RetentionDays > 0)
            {
                history.ExpiresAt = DateTime.UtcNow.AddDays(_options.RetentionDays);
            }

            await collection.InsertOneAsync(history, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record job execution veto for {JobKey}", context.JobDetail.Key);
        }
    }
}

/// <summary>
/// Options for configuring job execution history tracking.
/// </summary>
public class JobHistoryOptions
{
    /// <summary>
    /// Whether job history tracking is enabled. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Name of the MongoDB collection to store history. Default: "job_execution_history"
    /// </summary>
    public string CollectionName { get; set; } = "job_execution_history";

    /// <summary>
    /// Number of days to retain history records. 0 = no automatic cleanup. Default: 30
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Optional function to provide custom metadata for each job execution.
    /// </summary>
    public Func<IJobExecutionContext, Task<Dictionary<string, object>?>>? MetadataProvider { get; set; }

    /// <summary>
    /// Whether to create TTL index for automatic cleanup. Default: true
    /// </summary>
    public bool CreateTTLIndex { get; set; } = true;
}

/// <summary>
/// Extension methods for configuring job history.
/// </summary>
public static class JobHistoryExtensions
{
    /// <summary>
    /// Adds job execution history tracking to the Quartz scheduler.
    /// </summary>
    public static async Task AddJobHistoryTracking(
        this IScheduler scheduler,
        IServiceProvider serviceProvider,
        JobHistoryOptions? options = null)
    {
        options ??= new JobHistoryOptions();

        var logger = serviceProvider.GetRequiredService<ILogger<JobExecutionHistoryListener>>();
        var listener = new JobExecutionHistoryListener(
            serviceProvider,
            logger,
            options,
            scheduler.SchedulerInstanceId);

        scheduler.ListenerManager.AddJobListener(listener);

        // Create TTL index if configured
        if (options.CreateTTLIndex && options.RetentionDays > 0)
        {
            using var scope = serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            var collection = database.GetCollection<JobExecutionHistory>(options.CollectionName);

            var indexModel = new CreateIndexModel<JobExecutionHistory>(
                Builders<JobExecutionHistory>.IndexKeys.Ascending(h => h.ExpiresAt),
                new CreateIndexOptions
                {
                    Name = "TTL_ExpiresAt",
                    ExpireAfter = TimeSpan.Zero,
                    Background = true
                });

            try
            {
                await collection.Indexes.CreateOneAsync(indexModel);
                logger.LogInformation(
                    "Created TTL index on {Collection} for automatic history cleanup after {Days} days",
                    options.CollectionName,
                    options.RetentionDays);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create TTL index for job history");
            }
        }
    }
}