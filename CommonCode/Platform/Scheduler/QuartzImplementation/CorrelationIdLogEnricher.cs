using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Logger enricher that adds correlation IDs to all log entries.
/// </summary>
public class CorrelationIdLogEnricher
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// Gets or sets the current correlation ID.
    /// </summary>
    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// Creates a new correlation ID if one doesn't exist.
    /// </summary>
    public static string EnsureCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId.Value))
        {
            _correlationId.Value = Guid.NewGuid().ToString();
        }
        return _correlationId.Value;
    }

    /// <summary>
    /// Clears the current correlation ID.
    /// </summary>
    public static void Clear()
    {
        _correlationId.Value = null!;
    }
}

/// <summary>
/// Middleware for ASP.NET Core to extract or create correlation IDs from HTTP requests.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        CorrelationIdLogEnricher.CorrelationId = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                CorrelationIdLogEnricher.Clear();
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Base class for Quartz jobs that automatically manages correlation IDs.
/// </summary>
public abstract class CorrelatedJob : IJob
{
    protected ILogger Logger { get; }

    protected CorrelatedJob(ILogger logger)
    {
        Logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // Get or create correlation ID
        var correlationId = context.MergedJobDataMap.GetString("CorrelationId") 
            ?? Guid.NewGuid().ToString();
        
        CorrelationIdLogEnricher.CorrelationId = correlationId;
        
        // Store in job data for future executions
        context.JobDetail.JobDataMap["CorrelationId"] = correlationId;

        // Create logging scope with job metadata
        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["JobKey"] = context.JobDetail.Key.ToString(),
            ["JobType"] = GetType().Name,
            ["FireInstanceId"] = context.FireInstanceId,
            ["ScheduledFireTime"] = context.ScheduledFireTimeUtc?.ToString("O") ?? "",
            ["ActualFireTime"] = context.FireTimeUtc.ToString("O"),
            ["JobRunTime"] = context.JobRunTime.TotalMilliseconds,
            ["RefireCount"] = context.RefireCount,
            ["Recovering"] = context.Recovering
        }))
        {
            Logger.LogInformation(
                "Starting job execution with CorrelationId: {CorrelationId}",
                correlationId);

            try
            {
                await ExecuteJob(context);
                
                Logger.LogInformation(
                    "Job execution completed successfully. Duration: {Duration}ms",
                    context.JobRunTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Job execution failed after {Duration}ms",
                    context.JobRunTime.TotalMilliseconds);
                throw;
            }
            finally
            {
                CorrelationIdLogEnricher.Clear();
            }
        }
    }

    /// <summary>
    /// Implement this method to define the job's work.
    /// </summary>
    protected abstract Task ExecuteJob(IJobExecutionContext context);
}

/// <summary>
/// Extension methods for configuring correlation ID support.
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the ASP.NET Core pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Creates a logger scope with the current correlation ID.
    /// </summary>
    public static IDisposable? BeginCorrelationScope(this ILogger logger)
    {
        var correlationId = CorrelationIdLogEnricher.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });
        }
        return null;
    }

    /// <summary>
    /// Adds correlation ID to Quartz job data.
    /// </summary>
    public static JobDataMap WithCorrelationId(this JobDataMap jobData, string? correlationId = null)
    {
        jobData["CorrelationId"] = correlationId ?? CorrelationIdLogEnricher.EnsureCorrelationId();
        return jobData;
    }

    /// <summary>
    /// Adds correlation ID to a job builder.
    /// </summary>
    public static IJobConfigurator WithCorrelationId(this IJobConfigurator jobBuilder, string? correlationId = null)
    {
        jobBuilder.UsingJobData("CorrelationId", correlationId ?? CorrelationIdLogEnricher.EnsureCorrelationId());
        return jobBuilder;
    }
}