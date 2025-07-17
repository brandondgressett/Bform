# Quartz.NET Migration Guide for BFormDomain

## Overview

This guide provides step-by-step instructions for migrating from the current custom scheduler implementation to Quartz.NET with MongoDB persistence.

## Current vs. New Architecture

### Current Implementation
- **SchedulerBackgroundWorker**: Polls MongoDB continuously
- **NCrontab**: Parses cron expressions only
- **ScheduledJobEntity**: Stored in MongoDB with custom schema
- **AppEventSink**: Event-based job execution
- **Inefficient**: Continuous database polling

### New Quartz.NET Implementation
- **Quartz Scheduler**: Efficient trigger-based scheduling
- **MongoDB Job Store**: Native Quartz persistence
- **IJob Interface**: Standard job execution pattern
- **Clustering Support**: Multi-instance coordination
- **Rich Features**: Misfires, priorities, calendars, etc.

## Migration Steps

### Step 1: Install the New Implementation

1. The Quartz packages are already in your project file.
2. Copy the `QuartzImplementation` folder to your project.
3. Update your dependency injection configuration.

### Step 2: Configure Services

In your `Program.cs` or startup configuration:

```csharp
// Remove or comment out the old scheduler registration
// services.AddHostedService<SchedulerBackgroundWorker>();
// services.AddScoped<SchedulerLogic>();

// Add the new Quartz scheduler
services.AddBFormQuartzScheduler(
    mongoConnectionString: Configuration.GetConnectionString("MongoDB"),
    config =>
    {
        config.SchedulerName = "BFormScheduler";
        config.DatabaseName = "bform"; // Your existing database
        config.CollectionPrefix = "quartz_"; // Avoid conflicts with existing collections
        config.EnableClustering = true; // For multi-instance support
    });
```

### Step 3: Create Migration Command

Create a command to migrate existing scheduled jobs:

```csharp
using Cli.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Quartz;

[Command("scheduler", "migrate", "Migrate existing scheduled jobs to Quartz")]
public class MigrateSchedulerCommand : ICommand
{
    private readonly IMongoDatabase _database;
    private readonly IScheduler _quartzScheduler;
    private readonly ISchedulerLogic _newScheduler;
    private readonly ILogger<MigrateSchedulerCommand> _logger;

    public MigrateSchedulerCommand(
        IMongoDatabase database,
        IScheduler quartzScheduler,
        ISchedulerLogic newScheduler,
        ILogger<MigrateSchedulerCommand> logger)
    {
        _database = database;
        _quartzScheduler = quartzScheduler;
        _newScheduler = newScheduler;
        _logger = logger;
    }

    public async Task<object> ExecuteAsync(
        Dictionary<string, object?> args, 
        CommandContext context, 
        CancellationToken cancellationToken)
    {
        var dryRun = args.GetValueOrDefault("dryRun", false) as bool? ?? false;
        var migratedCount = 0;
        var errorCount = 0;

        try
        {
            // Get existing scheduled jobs from MongoDB
            var collection = _database.GetCollection<ScheduledJobEntity>("ScheduledJobEntity");
            var existingJobs = await collection.Find(_ => true).ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} existing scheduled jobs", existingJobs.Count);

            foreach (var oldJob in existingJobs)
            {
                try
                {
                    if (!dryRun)
                    {
                        await MigrateJob(oldJob, cancellationToken);
                    }
                    
                    migratedCount++;
                    _logger.LogInformation(
                        "Migrated job {JobId} - {JobName}", 
                        oldJob.Id, oldJob.Payload?.Name);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, 
                        "Failed to migrate job {JobId}", oldJob.Id);
                }
            }

            return new
            {
                success = true,
                totalJobs = existingJobs.Count,
                migratedCount,
                errorCount,
                dryRun,
                message = dryRun 
                    ? "Dry run completed. Run without --dry-run to perform migration." 
                    : "Migration completed successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            throw;
        }
    }

    private async Task MigrateJob(ScheduledJobEntity oldJob, CancellationToken cancellationToken)
    {
        // Create a Quartz job
        var jobKey = new JobKey(oldJob.Id.ToString(), "migrated");
        var job = JobBuilder.Create<QuartzSinkEventJob>()
            .WithIdentity(jobKey)
            .WithDescription($"Migrated: {oldJob.Payload?.Name}")
            .UsingJobData("eventData", JsonConvert.SerializeObject(oldJob.Payload))
            .StoreDurably(false)
            .Build();

        // Create trigger based on old job configuration
        ITrigger trigger;
        
        if (!string.IsNullOrEmpty(oldJob.Payload?.CronExpression))
        {
            // Cron schedule
            trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{oldJob.Id}", "migrated")
                .ForJob(jobKey)
                .StartAt(oldJob.NextDeadline)
                .WithCronSchedule(oldJob.Payload.CronExpression)
                .Build();
        }
        else if (oldJob.RecurrenceSchedule != TimeSpan.Zero)
        {
            // Simple schedule
            var builder = TriggerBuilder.Create()
                .WithIdentity($"trigger_{oldJob.Id}", "migrated")
                .ForJob(jobKey)
                .StartAt(oldJob.NextDeadline)
                .WithSimpleSchedule(x => x
                    .WithInterval(oldJob.RecurrenceSchedule)
                    .WithMisfireHandlingInstructionFireNow());

            // Handle repeat count
            if (oldJob.Payload?.Type == ScheduleType.RecurringX)
            {
                builder.WithSimpleSchedule(x => x
                    .WithInterval(oldJob.RecurrenceSchedule)
                    .WithRepeatCount(oldJob.Payload.RecurrenceCount - oldJob.Payload.InvocationCount)
                    .WithMisfireHandlingInstructionFireNow());
            }
            else if (oldJob.Payload?.Type == ScheduleType.RecurringInfinite)
            {
                builder.WithSimpleSchedule(x => x
                    .WithInterval(oldJob.RecurrenceSchedule)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionFireNow());
            }

            trigger = builder.Build();
        }
        else
        {
            // One-time trigger
            trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{oldJob.Id}", "migrated")
                .ForJob(jobKey)
                .StartAt(oldJob.NextDeadline)
                .Build();
        }

        // Schedule in Quartz
        await _quartzScheduler.ScheduleJob(job, trigger, cancellationToken);
    }
}
```

### Step 4: Run Migration

1. **Test in Development First**
   ```bash
   dotnet run -- scheduler migrate --dry-run
   ```

2. **Perform Actual Migration**
   ```bash
   dotnet run -- scheduler migrate
   ```

3. **Verify Migration**
   ```bash
   # Create a command to list Quartz jobs
   dotnet run -- scheduler list
   ```

### Step 5: Update Job Creation Code

Replace calls to the old `SchedulerLogic` with the new `ISchedulerLogic`:

```csharp
// Old code
var schedulerLogic = services.GetService<SchedulerLogic>();
var identifier = await schedulerLogic.EventScheduleEvents(...);

// New code
var schedulerLogic = services.GetService<ISchedulerLogic>();
var identifier = await schedulerLogic.EventScheduleEventsAsync(...);
```

### Step 6: Parallel Running (Optional)

For safety, you can run both schedulers in parallel:

1. Keep the old `SchedulerBackgroundWorker` running
2. Add a flag to prevent duplicate executions
3. Gradually migrate jobs to Quartz
4. Monitor both systems
5. Disable old scheduler once confident

### Step 7: Cleanup

Once migration is complete:

1. Remove old scheduler code:
   - `SchedulerBackgroundWorker.cs`
   - Old `SchedulerLogic.cs`
   - `ScheduledJobEntity` collection (after backup)

2. Update documentation

3. Remove NCrontab package (Quartz has built-in cron support)

## Monitoring and Troubleshooting

### Check Quartz Tables/Collections

Quartz will create these MongoDB collections:
- `quartz_jobs` - Job definitions
- `quartz_triggers` - Trigger definitions
- `quartz_calendars` - Calendar definitions
- `quartz_paused_trigger_groups` - Paused groups
- `quartz_fired_triggers` - Currently executing
- `quartz_scheduler_state` - Cluster state
- `quartz_locks` - Distributed locks
- `quartz_simple_triggers` - Simple trigger data
- `quartz_cron_triggers` - Cron trigger data
- `quartz_blob_triggers` - Custom trigger data

### Common Issues

1. **Jobs Not Firing**
   - Check if scheduler is started
   - Verify trigger configuration
   - Check for misfires

2. **Duplicate Executions**
   - Ensure old scheduler is disabled
   - Check clustering configuration

3. **Performance Issues**
   - Adjust thread pool size
   - Check MongoDB indexes
   - Monitor connection pool

### Monitoring Commands

Create these helpful commands:

```csharp
[Command("scheduler", "status", "Show scheduler status")]
public class SchedulerStatusCommand : ICommand
{
    public async Task<object> ExecuteAsync(...)
    {
        var metadata = await _scheduler.GetMetaData();
        return new
        {
            schedulerName = metadata.SchedulerName,
            schedulerInstanceId = metadata.SchedulerInstanceId,
            schedulerType = metadata.SchedulerType,
            started = metadata.Started,
            inStandbyMode = metadata.InStandbyMode,
            shutdown = metadata.Shutdown,
            jobsExecuted = metadata.NumberOfJobsExecuted,
            runningSince = metadata.RunningSince,
            threadPoolSize = metadata.ThreadPoolSize,
            version = metadata.Version
        };
    }
}

[Command("scheduler", "jobs", "List all scheduled jobs")]
public class ListJobsCommand : ICommand
{
    public async Task<object> ExecuteAsync(...)
    {
        var jobs = await _schedulerLogic.ListJobsAsync();
        return jobs;
    }
}
```

## Rollback Plan

If issues arise:

1. **Immediate Rollback**
   - Stop Quartz scheduler
   - Re-enable old SchedulerBackgroundWorker
   - Restore from backup if needed

2. **Data Preservation**
   - Quartz collections are separate
   - Original ScheduledJobEntity unchanged
   - Can run migration again

## Benefits After Migration

1. **Performance**: 10-100x reduction in database queries
2. **Reliability**: Built-in misfire handling
3. **Scalability**: Clustering support
4. **Features**: Job priorities, calendars, listeners
5. **Monitoring**: Rich job execution history
6. **Standards**: Industry-standard scheduling