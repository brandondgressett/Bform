using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Quartz.Spi.MongoDbJobStore;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Configures and manages Quartz.NET with MongoDB job persistence.
/// This replaces the custom SchedulerBackgroundWorker implementation.
/// </summary>
public static class QuartzSchedulerService
{
    /// <summary>
    /// Adds Quartz.NET with MongoDB job store to the service collection.
    /// </summary>
    public static IServiceCollection AddBFormQuartzScheduler(
        this IServiceCollection services,
        string mongoConnectionString,
        Action<QuartzConfiguration>? configureOptions = null,
        Action<QuartzHealthCheckOptions>? configureHealthCheck = null,
        Action<JobHistoryOptions>? configureHistory = null)
    {
        var config = new QuartzConfiguration();
        configureOptions?.Invoke(config);

        // Extract database name from connection string if not provided
        if (string.IsNullOrEmpty(config.DatabaseName))
        {
            var uri = new MongoDB.Driver.MongoUrl(mongoConnectionString);
            config.DatabaseName = uri.DatabaseName ?? "quartz";
        }

        // Traditional configuration approach (more reliable with MongoDB)
        var properties = new NameValueCollection
        {
            // Scheduler configuration
            ["quartz.scheduler.instanceName"] = config.SchedulerName,
            ["quartz.scheduler.instanceId"] = config.SchedulerId,
            ["quartz.scheduler.makeSchedulerThreadDaemon"] = "true",
            
            // ThreadPool configuration
            ["quartz.threadPool.type"] = "Quartz.Simpl.DefaultThreadPool, Quartz",
            ["quartz.threadPool.threadCount"] = config.ThreadCount.ToString(),
            ["quartz.threadPool.threadPriority"] = "Normal",
            
            // JobStore configuration - MongoDB
            ["quartz.jobStore.type"] = "Quartz.Spi.MongoDbJobStore.MongoDbJobStore, Quartz.Spi.MongoDbJobStore",
            ["quartz.jobStore.connectionString"] = mongoConnectionString,
            ["quartz.jobStore.databaseName"] = config.DatabaseName,
            ["quartz.jobStore.collectionPrefix"] = config.CollectionPrefix,
            
            // Clustering support
            ["quartz.jobStore.clustered"] = config.EnableClustering.ToString().ToLower(),
            ["quartz.jobStore.clusterCheckinInterval"] = config.ClusterCheckinInterval.ToString(),
            
            // Serialization
            ["quartz.serializer.type"] = "json",
            
            // Misfire handling
            ["quartz.jobStore.misfireThreshold"] = config.MisfireThreshold.ToString()
        };

        // Register the scheduler factory
        services.AddSingleton<ISchedulerFactory>(provider =>
        {
            var factory = new StdSchedulerFactory(properties);
            return factory;
        });

        // Register the scheduler
        services.AddSingleton<IScheduler>(provider =>
        {
            var factory = provider.GetRequiredService<ISchedulerFactory>();
            var scheduler = factory.GetScheduler().GetAwaiter().GetResult();
            
            // Configure job factory to use DI
            scheduler.JobFactory = new MicrosoftDependencyInjectionJobFactory(provider);
            
            return scheduler;
        });

        // Register the hosted service to start/stop the scheduler
        services.AddHostedService<QuartzHostedService>();

        // Register job types
        services.AddScoped<QuartzSinkEventJob>();
        
        // Register the generic wrapper for IJobIntegration jobs
        services.AddScoped(typeof(QuartzJobIntegrationWrapper<>));
        
        // Register the new scheduler logic that uses Quartz
        services.AddScoped<QuartzISchedulerLogic, QuartzSchedulerLogic>();

        // Register health check
        var healthCheckOptions = new QuartzHealthCheckOptions();
        configureHealthCheck?.Invoke(healthCheckOptions);
        
        services.AddSingleton(healthCheckOptions);
        services.AddHealthChecks()
            .AddTypeActivatedCheck<QuartzSchedulerHealthCheck>(
                "quartz_scheduler",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "scheduler", "quartz", "ready" });

        // Register job history tracking if configured
        if (configureHistory != null)
        {
            var historyOptions = new JobHistoryOptions();
            configureHistory(historyOptions);
            services.AddSingleton(historyOptions);
            
            // Register MongoDB database for history if not already registered
            services.TryAddSingleton<IMongoDatabase>(provider =>
            {
                var client = new MongoDB.Driver.MongoClient(mongoConnectionString);
                var url = new MongoDB.Driver.MongoUrl(mongoConnectionString);
                return client.GetDatabase(url.DatabaseName ?? config.DatabaseName);
            });
        }

        return services;
    }
}

/// <summary>
/// Configuration options for Quartz scheduler.
/// </summary>
public class QuartzConfiguration
{
    public string SchedulerName { get; set; } = "BFormScheduler";
    public string SchedulerId { get; set; } = "AUTO";
    public string DatabaseName { get; set; } = "";
    public string CollectionPrefix { get; set; } = "quartz_";
    public int ThreadCount { get; set; } = 10;
    public bool EnableClustering { get; set; } = false;
    public int ClusterCheckinInterval { get; set; } = 15000; // milliseconds
    public int MisfireThreshold { get; set; } = 60000; // milliseconds
}

/// <summary>
/// Job factory that integrates with Microsoft.Extensions.DependencyInjection.
/// </summary>
public class MicrosoftDependencyInjectionJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MicrosoftDependencyInjectionJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        
        // Create a scope for the job
        var scope = _serviceProvider.CreateScope();
        var job = (IJob)scope.ServiceProvider.GetRequiredService(jobType);
        
        // Wrap the job to dispose the scope after execution
        return new ScopedJob(job, scope);
    }

    public void ReturnJob(IJob job)
    {
        // The scope is disposed by ScopedJob
    }
}

/// <summary>
/// Wraps a job to ensure the DI scope is disposed after execution.
/// </summary>
internal class ScopedJob : IJob
{
    private readonly IJob _innerJob;
    private readonly IServiceScope _scope;

    public ScopedJob(IJob innerJob, IServiceScope scope)
    {
        _innerJob = innerJob;
        _scope = scope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _innerJob.Execute(context);
        }
        finally
        {
            _scope.Dispose();
        }
    }
}

/// <summary>
/// Hosted service that manages the Quartz scheduler lifecycle.
/// </summary>
public class QuartzHostedService : IHostedService
{
    private readonly IScheduler _scheduler;
    private readonly ILogger<QuartzHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public QuartzHostedService(
        IScheduler scheduler,
        ILogger<QuartzHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _scheduler = scheduler;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Quartz scheduler...");
        
        if (!_scheduler.IsStarted)
        {
            // Add job history tracking if configured
            var historyOptions = _serviceProvider.GetService<JobHistoryOptions>();
            if (historyOptions?.Enabled == true)
            {
                await _scheduler.AddJobHistoryTracking(_serviceProvider, historyOptions);
                _logger.LogInformation("Job history tracking enabled");
            }
            
            await _scheduler.Start(cancellationToken);
        }
        
        _logger.LogInformation("Quartz scheduler started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Quartz scheduler...");
        
        if (!_scheduler.IsShutdown)
        {
            // Wait for jobs to complete
            await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
        }
        
        _logger.LogInformation("Quartz scheduler stopped");
    }
}