using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Health check for Quartz scheduler to monitor its status and performance.
/// </summary>
public class QuartzSchedulerHealthCheck : IHealthCheck
{
    private readonly IScheduler _scheduler;
    private readonly ILogger<QuartzSchedulerHealthCheck> _logger;
    private readonly QuartzHealthCheckOptions _options;

    public QuartzSchedulerHealthCheck(
        IScheduler scheduler,
        ILogger<QuartzSchedulerHealthCheck> logger,
        QuartzHealthCheckOptions? options = null)
    {
        _scheduler = scheduler;
        _logger = logger;
        _options = options ?? new QuartzHealthCheckOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // Check if scheduler is started
            if (!_scheduler.IsStarted)
            {
                return HealthCheckResult.Unhealthy("Scheduler is not started", data: data);
            }

            // Check if scheduler is shutdown
            if (_scheduler.IsShutdown)
            {
                return HealthCheckResult.Unhealthy("Scheduler is shutdown", data: data);
            }

            // Get scheduler metadata
            var metaData = await _scheduler.GetMetaData(cancellationToken);
            data["SchedulerName"] = _scheduler.SchedulerName;
            data["SchedulerInstanceId"] = _scheduler.SchedulerInstanceId;
            data["RunningSince"] = metaData.RunningSince?.ToString("O") ?? "Not started";
            data["NumberOfJobsExecuted"] = metaData.NumberOfJobsExecuted;
            data["ThreadPoolSize"] = metaData.ThreadPoolSize;
            data["Version"] = metaData.Version;
            data["JobStoreType"] = metaData.JobStoreType.Name;
            data["InStandbyMode"] = _scheduler.InStandbyMode;

            // Get currently executing jobs
            var executingJobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            data["CurrentlyExecutingJobs"] = executingJobs.Count;

            // Get all job groups
            var jobGroups = await _scheduler.GetJobGroupNames(cancellationToken);
            data["JobGroups"] = jobGroups.Count;

            // Count total jobs
            var totalJobs = 0;
            var pausedJobs = 0;
            foreach (var group in jobGroups)
            {
                var jobKeys = await _scheduler.GetJobKeys(
                    GroupMatcher<JobKey>.GroupEquals(group), 
                    cancellationToken);
                totalJobs += jobKeys.Count;

                // Check for paused jobs
                foreach (var jobKey in jobKeys)
                {
                    var triggers = await _scheduler.GetTriggersOfJob(jobKey, cancellationToken);
                    foreach (var trigger in triggers)
                    {
                        var state = await _scheduler.GetTriggerState(trigger.Key, cancellationToken);
                        if (state == TriggerState.Paused)
                        {
                            pausedJobs++;
                        }
                    }
                }
            }
            data["TotalJobs"] = totalJobs;
            data["PausedJobs"] = pausedJobs;

            // Get trigger groups
            var triggerGroups = await _scheduler.GetTriggerGroupNames(cancellationToken);
            data["TriggerGroups"] = triggerGroups.Count;

            // Check for misfired jobs (if threshold is set)
            if (_options.CheckMisfiredJobs)
            {
                var misfiredCount = await CheckMisfiredJobsAsync(cancellationToken);
                data["MisfiredJobs"] = misfiredCount;

                if (misfiredCount > _options.MaxMisfiredJobs)
                {
                    return HealthCheckResult.Degraded(
                        $"Too many misfired jobs: {misfiredCount} > {_options.MaxMisfiredJobs}",
                        data: data);
                }
            }

            // Check thread pool usage
            var threadPoolUsage = executingJobs.Count / (double)metaData.ThreadPoolSize;
            data["ThreadPoolUsage"] = $"{threadPoolUsage:P0}";

            if (threadPoolUsage > _options.MaxThreadPoolUsage)
            {
                return HealthCheckResult.Degraded(
                    $"Thread pool usage too high: {threadPoolUsage:P0}",
                    data: data);
            }

            // Check if scheduler is in standby mode
            if (_scheduler.InStandbyMode)
            {
                return HealthCheckResult.Degraded("Scheduler is in standby mode", data: data);
            }

            return HealthCheckResult.Healthy("Quartz scheduler is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Quartz scheduler health");
            
            var data = new Dictionary<string, object>
            {
                ["Exception"] = ex.GetType().Name,
                ["Error"] = ex.Message
            };

            return HealthCheckResult.Unhealthy(
                "Error checking scheduler health",
                exception: ex,
                data: data);
        }
    }

    private async Task<int> CheckMisfiredJobsAsync(CancellationToken cancellationToken)
    {
        var misfiredCount = 0;
        var now = DateTimeOffset.UtcNow;

        var groups = await _scheduler.GetTriggerGroupNames(cancellationToken);
        foreach (var group in groups)
        {
            var triggers = await _scheduler.GetTriggerKeys(
                GroupMatcher<TriggerKey>.GroupEquals(group), 
                cancellationToken);

            foreach (var triggerKey in triggers)
            {
                var trigger = await _scheduler.GetTrigger(triggerKey, cancellationToken);
                if (trigger != null)
                {
                    var state = await _scheduler.GetTriggerState(triggerKey, cancellationToken);
                    
                    // Check if trigger should have fired but hasn't
                    var nextFireTime = trigger.GetNextFireTimeUtc();
                    if (state == TriggerState.Normal && 
                        nextFireTime.HasValue &&
                        nextFireTime.Value < now.AddSeconds(-_options.MisfireThresholdSeconds))
                    {
                        misfiredCount++;
                    }
                }
            }
        }

        return misfiredCount;
    }
}

/// <summary>
/// Options for configuring Quartz health check behavior.
/// </summary>
public class QuartzHealthCheckOptions
{
    /// <summary>
    /// Maximum thread pool usage percentage before reporting degraded. Default: 0.9 (90%)
    /// </summary>
    public double MaxThreadPoolUsage { get; set; } = 0.9;

    /// <summary>
    /// Whether to check for misfired jobs. Default: true
    /// </summary>
    public bool CheckMisfiredJobs { get; set; } = true;

    /// <summary>
    /// Maximum number of misfired jobs before reporting degraded. Default: 5
    /// </summary>
    public int MaxMisfiredJobs { get; set; } = 5;

    /// <summary>
    /// Seconds past next fire time to consider a job misfired. Default: 60
    /// </summary>
    public int MisfireThresholdSeconds { get; set; } = 60;
}