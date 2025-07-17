using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Reflection;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// Generic wrapper that adapts IJobIntegration jobs to work with Quartz IJob interface.
/// </summary>
/// <typeparam name="TJob">The type of job that implements IJobIntegration.</typeparam>
[DisallowConcurrentExecution]
public class QuartzJobIntegrationWrapper<TJob> : IJob where TJob : IJobIntegration
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuartzJobIntegrationWrapper<TJob>> _logger;

    public QuartzJobIntegrationWrapper(
        IServiceProvider serviceProvider,
        ILogger<QuartzJobIntegrationWrapper<TJob>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobType = typeof(TJob);
        _logger.LogInformation(
            "Executing IJobIntegration job {JobType} with JobKey {JobKey}", 
            jobType.Name, 
            context.JobDetail.Key);

        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Create instance of the job
            var job = ActivatorUtilities.CreateInstance<TJob>(scope.ServiceProvider);
            
            // Pass job data to the job instance if it has settable properties
            var jobData = context.MergedJobDataMap;
            if (jobData != null && jobData.Count > 0)
            {
                var properties = jobType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

                foreach (var property in properties)
                {
                    if (jobData.ContainsKey(property.Name))
                    {
                        try
                        {
                            var value = jobData[property.Name];
                            if (value != null)
                            {
                                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                                property.SetValue(job, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, 
                                "Failed to set property {PropertyName} on job {JobType}", 
                                property.Name, 
                                jobType.Name);
                        }
                    }
                }
            }

            // Execute the job
            await job.Execute();
            
            _logger.LogInformation(
                "Successfully executed IJobIntegration job {JobType}", 
                jobType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error executing IJobIntegration job {JobType}", 
                jobType.Name);
            
            // Wrap in JobExecutionException for Quartz error handling
            throw new JobExecutionException(
                $"Error executing IJobIntegration job {jobType.Name}", 
                ex, 
                false); // Don't refire immediately
        }
    }
}

/// <summary>
/// Factory for creating QuartzJobIntegrationWrapper instances.
/// </summary>
public static class QuartzJobIntegrationWrapperFactory
{
    /// <summary>
    /// Creates a job detail for an IJobIntegration job wrapped for Quartz.
    /// </summary>
    public static IJobDetail CreateJobDetail<TJob>(
        string jobName,
        string groupName,
        JobDataMap? jobData = null,
        string? description = null) where TJob : IJobIntegration
    {
        var jobBuilder = JobBuilder.Create<QuartzJobIntegrationWrapper<TJob>>()
            .WithIdentity(jobName, groupName)
            .StoreDurably();

        if (!string.IsNullOrEmpty(description))
        {
            jobBuilder.WithDescription(description);
        }

        if (jobData != null)
        {
            jobBuilder.UsingJobData(jobData);
        }

        // Store the actual job type for reference
        jobBuilder.UsingJobData("ActualJobType", typeof(TJob).AssemblyQualifiedName);

        return jobBuilder.Build();
    }
}