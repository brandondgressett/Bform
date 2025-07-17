using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Quartz.NET implementation of the SinkEventJob.
/// This job implements IJob interface for proper Quartz integration.
/// </summary>
[DisallowConcurrentExecution] // Prevents multiple instances of the same job from running simultaneously
[PersistJobDataAfterExecution] // Ensures job data changes are persisted
public class QuartzSinkEventJob : IJob
{
    private readonly AppEventSink _sink;
    private readonly IApplicationAlert _alerts;
    private readonly IDataEnvironment _env;
    private readonly ILogger<QuartzSinkEventJob> _logger;
    private readonly ITenantContext _tenantContext;

    // Quartz will inject these dependencies via the DI-aware job factory
    public QuartzSinkEventJob(
        AppEventSink sink,
        IApplicationAlert alerts,
        IDataEnvironment env,
        ILogger<QuartzSinkEventJob> logger,
        ITenantContext tenantContext)
    {
        _sink = sink;
        _alerts = alerts;
        _env = env;
        _logger = logger;
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Executes the scheduled job to sink an app event.
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        using (PerfTrack.Stopwatch(nameof(QuartzSinkEventJob)))
        {
            var jobKey = context.JobDetail.Key;
            _logger.LogDebug("Executing job {JobName} with key {JobKey}", 
                nameof(QuartzSinkEventJob), jobKey);

            try
            {
                // Retrieve the scheduled event data from job data map
                var eventDataJson = context.JobDetail.JobDataMap.GetString("eventData");
                if (string.IsNullOrWhiteSpace(eventDataJson))
                {
                    throw new JobExecutionException("Missing eventData in job data map");
                }

                var scheduledEvent = JsonConvert.DeserializeObject<ScheduledEvent>(eventDataJson);
                if (scheduledEvent == null)
                {
                    throw new JobExecutionException("Failed to deserialize scheduled event");
                }

                // Set tenant context for this job execution
                var tenantId = context.JobDetail.JobDataMap.GetString("tenantId");
                if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tenantGuid))
                {
                    _tenantContext.SetCurrentTenant(tenantGuid);
                    _logger.LogDebug("Set tenant context to {TenantId} for job {JobKey}", tenantId, jobKey);
                }
                else
                {
                    _logger.LogDebug("No tenant context for job {JobKey} (global/system job)", jobKey);
                }

                // Open transaction and process the event
                using var transaction = await _env.OpenTransactionAsync(context.CancellationToken);
                _sink.BeginBatch(transaction);

                await _sink.Enqueue(
                    new AppEventOrigin(nameof(QuartzSinkEventJob), jobKey.ToString(), null),
                    scheduledEvent.EventTopic,
                    null,
                    scheduledEvent,
                    null,
                    scheduledEvent.Tags,
                    false);

                await _sink.CommitBatch();
                
                _logger.LogInformation(
                    "Successfully executed scheduled event {EventId} for topic {Topic}",
                    scheduledEvent.Id, scheduledEvent.EventTopic);

                // Store execution result in job data map for monitoring
                context.JobDetail.JobDataMap["lastExecutionTime"] = DateTime.UtcNow.ToString("O");
                context.JobDetail.JobDataMap["lastExecutionStatus"] = "Success";
            }
            catch (JobExecutionException)
            {
                // Re-throw job execution exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error executing scheduled job {JobKey}", jobKey);

                _alerts.RaiseAlert(
                    ApplicationAlertKind.General,
                    LogLevel.Error,
                    $"Scheduled job execution failed: {ex.Message}",
                    5);

                // Store error information
                context.JobDetail.JobDataMap["lastExecutionTime"] = DateTime.UtcNow.ToString("O");
                context.JobDetail.JobDataMap["lastExecutionStatus"] = "Failed";
                context.JobDetail.JobDataMap["lastExecutionError"] = ex.Message;

                // Wrap in JobExecutionException to let Quartz handle retry logic
                var jobEx = new JobExecutionException(
                    $"Job execution failed: {ex.Message}", 
                    ex, 
                    refireImmediately: false);

                // Configure retry behavior based on error type
                if (IsTransientError(ex))
                {
                    // For transient errors, retry with exponential backoff
                    var retryCount = context.JobDetail.JobDataMap.GetInt("retryCount");
                    if (retryCount < 3)
                    {
                        context.JobDetail.JobDataMap["retryCount"] = retryCount + 1;
                        
                        // Calculate backoff delay: 1s, 2s, 4s
                        var delaySeconds = Math.Pow(2, retryCount);
                        
                        jobEx.RefireImmediately = false;
                        jobEx.UnscheduleFiringTrigger = false;
                        jobEx.UnscheduleAllTriggers = false;
                        
                        // Create a new trigger with delay
                        var retryTrigger = TriggerBuilder.Create()
                            .WithIdentity($"retry-{jobKey}-{retryCount}", jobKey.Group)
                            .ForJob(jobKey)
                            .StartAt(DateTimeOffset.UtcNow.AddSeconds(delaySeconds))
                            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                            .Build();
                            
                        // Schedule the retry
                        await context.Scheduler.ScheduleJob(retryTrigger);
                        
                        _logger.LogWarning(
                            "Transient error in job {JobKey}, scheduling retry {RetryCount} in {DelaySeconds}s",
                            jobKey, retryCount + 1, delaySeconds);
                    }
                    else
                    {
                        _logger.LogError(
                            "Job {JobKey} failed after {MaxRetries} retries",
                            jobKey, retryCount);
                    }
                }

                throw jobEx;
            }
        }
    }

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    private bool IsTransientError(Exception ex)
    {
        // MongoDB transient errors
        if (ex is MongoDB.Driver.MongoException mongoEx)
        {
            return mongoEx is MongoDB.Driver.MongoConnectionException ||
                   mongoEx is MongoDB.Driver.MongoServerException;
        }

        // Network-related errors
        if (ex is System.Net.Http.HttpRequestException ||
            ex is System.Net.Sockets.SocketException ||
            ex is TimeoutException)
        {
            return true;
        }

        // Check inner exceptions
        if (ex.InnerException != null)
        {
            return IsTransientError(ex.InnerException);
        }

        return false;
    }
}