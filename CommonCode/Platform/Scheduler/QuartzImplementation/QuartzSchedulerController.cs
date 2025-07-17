using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl.Matchers;

namespace BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;

/// <summary>
/// REST API controller for managing Quartz scheduler operations.
/// </summary>
[ApiController]
[Route("api/scheduler")]
[Authorize] // Require authentication for all endpoints
public class QuartzSchedulerController : ControllerBase
{
    private readonly QuartzISchedulerLogic _schedulerLogic;
    private readonly IScheduler _scheduler;
    private readonly ILogger<QuartzSchedulerController> _logger;

    public QuartzSchedulerController(
        QuartzISchedulerLogic schedulerLogic,
        IScheduler scheduler,
        ILogger<QuartzSchedulerController> logger)
    {
        _schedulerLogic = schedulerLogic;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <summary>
    /// Get scheduler status and metadata.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var metadata = await _scheduler.GetMetaData();
            var status = new
            {
                schedulerName = _scheduler.SchedulerName,
                schedulerInstanceId = _scheduler.SchedulerInstanceId,
                schedulerType = metadata.SchedulerType?.Name,
                started = _scheduler.IsStarted,
                inStandbyMode = _scheduler.InStandbyMode,
                shutdown = _scheduler.IsShutdown,
                runningSince = metadata.RunningSince?.ToString("O"),
                numberOfJobsExecuted = metadata.NumberOfJobsExecuted,
                jobStoreType = metadata.JobStoreType?.Name,
                threadPoolSize = metadata.ThreadPoolSize,
                version = metadata.Version
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduler status");
            return StatusCode(500, new { error = "Failed to get scheduler status" });
        }
    }

    /// <summary>
    /// List all scheduled jobs with optional filtering.
    /// </summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> ListJobs(
        [FromQuery] string? group = null,
        [FromQuery] bool includeCompleted = false)
    {
        try
        {
            var jobs = await _schedulerLogic.ListJobsAsync(group, includeCompleted);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jobs");
            return StatusCode(500, new { error = "Failed to list jobs" });
        }
    }

    /// <summary>
    /// Get details for a specific job.
    /// </summary>
    [HttpGet("jobs/{scheduleId}")]
    public async Task<IActionResult> GetJob(string scheduleId)
    {
        try
        {
            var job = await _schedulerLogic.GetJobInfoAsync(scheduleId);
            if (job == null)
            {
                return NotFound(new { error = $"Job {scheduleId} not found" });
            }

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "Failed to get job details" });
        }
    }

    /// <summary>
    /// Create a new scheduled job.
    /// </summary>
    [HttpPost("jobs")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            var template = new ScheduledEventTemplate
            {
                Name = request.Name,
                Group = request.Group ?? "default",
                Schedule = request.Schedule,
                EventTopic = request.EventType,
                EndAfter = request.EndAfter,
                Tags = request.Tags ?? new List<string>()
            };

            var content = request.Content ?? new JObject();
            
            var identifier = await _schedulerLogic.EventScheduleEventsAsync(
                template,
                content,
                request.HostWorkSet,
                request.HostWorkItem,
                request.AttachedItem,
                request.Tags);

            return CreatedAtAction(
                nameof(GetJob),
                new { scheduleId = identifier.JobId },
                new { scheduleId = identifier.JobId.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Failed to create job" });
        }
    }

    /// <summary>
    /// Delete a scheduled job.
    /// </summary>
    [HttpDelete("jobs/{scheduleId}")]
    public async Task<IActionResult> DeleteJob(string scheduleId)
    {
        try
        {
            var deleted = await _schedulerLogic.DescheduleAsync(scheduleId);
            if (!deleted)
            {
                return NotFound(new { error = $"Job {scheduleId} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job {ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "Failed to delete job" });
        }
    }

    /// <summary>
    /// Pause a scheduled job.
    /// </summary>
    [HttpPost("jobs/{scheduleId}/pause")]
    public async Task<IActionResult> PauseJob(string scheduleId)
    {
        try
        {
            await _schedulerLogic.PauseJobAsync(scheduleId);
            return Ok(new { message = $"Job {scheduleId} paused" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing job {ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "Failed to pause job" });
        }
    }

    /// <summary>
    /// Resume a paused job.
    /// </summary>
    [HttpPost("jobs/{scheduleId}/resume")]
    public async Task<IActionResult> ResumeJob(string scheduleId)
    {
        try
        {
            await _schedulerLogic.ResumeJobAsync(scheduleId);
            return Ok(new { message = $"Job {scheduleId} resumed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job {ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "Failed to resume job" });
        }
    }

    /// <summary>
    /// Trigger a job to run immediately.
    /// </summary>
    [HttpPost("jobs/{scheduleId}/trigger")]
    public async Task<IActionResult> TriggerJob(string scheduleId)
    {
        try
        {
            var triggered = await _schedulerLogic.TriggerJobNowAsync(scheduleId);
            if (!triggered)
            {
                return NotFound(new { error = $"Job {scheduleId} not found" });
            }

            return Ok(new { message = $"Job {scheduleId} triggered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering job {ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "Failed to trigger job" });
        }
    }

    /// <summary>
    /// Get currently executing jobs.
    /// </summary>
    [HttpGet("jobs/executing")]
    public async Task<IActionResult> GetExecutingJobs()
    {
        try
        {
            var executingJobs = await _scheduler.GetCurrentlyExecutingJobs();
            var jobs = executingJobs.Select(j => new
            {
                jobKey = j.JobDetail.Key.ToString(),
                triggerKey = j.Trigger.Key.ToString(),
                fireTime = j.FireTimeUtc.ToString("O"),
                scheduledFireTime = j.ScheduledFireTimeUtc?.ToString("O"),
                runTime = j.JobRunTime.TotalMilliseconds,
                refireCount = j.RefireCount,
                recovering = j.Recovering
            });

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executing jobs");
            return StatusCode(500, new { error = "Failed to get executing jobs" });
        }
    }

    /// <summary>
    /// Pause all jobs in a group.
    /// </summary>
    [HttpPost("groups/{groupName}/pause")]
    [Authorize(Roles = "Admin")] // Require admin role
    public async Task<IActionResult> PauseGroup(string groupName)
    {
        try
        {
            await _scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(groupName));
            return Ok(new { message = $"Group {groupName} paused" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing group {GroupName}", groupName);
            return StatusCode(500, new { error = "Failed to pause group" });
        }
    }

    /// <summary>
    /// Resume all jobs in a group.
    /// </summary>
    [HttpPost("groups/{groupName}/resume")]
    [Authorize(Roles = "Admin")] // Require admin role
    public async Task<IActionResult> ResumeGroup(string groupName)
    {
        try
        {
            await _scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(groupName));
            return Ok(new { message = $"Group {groupName} resumed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming group {GroupName}", groupName);
            return StatusCode(500, new { error = "Failed to resume group" });
        }
    }

    /// <summary>
    /// Put scheduler in standby mode.
    /// </summary>
    [HttpPost("standby")]
    [Authorize(Roles = "Admin")] // Require admin role
    public async Task<IActionResult> Standby()
    {
        try
        {
            await _scheduler.Standby();
            return Ok(new { message = "Scheduler is now in standby mode" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error putting scheduler in standby");
            return StatusCode(500, new { error = "Failed to enter standby mode" });
        }
    }

    /// <summary>
    /// Start the scheduler from standby mode.
    /// </summary>
    [HttpPost("start")]
    [Authorize(Roles = "Admin")] // Require admin role
    public async Task<IActionResult> Start()
    {
        try
        {
            await _scheduler.Start();
            return Ok(new { message = "Scheduler started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting scheduler");
            return StatusCode(500, new { error = "Failed to start scheduler" });
        }
    }

    /// <summary>
    /// Shutdown the scheduler.
    /// </summary>
    [HttpPost("shutdown")]
    [Authorize(Roles = "Admin")] // Require admin role
    public async Task<IActionResult> Shutdown([FromQuery] bool waitForJobsToComplete = true)
    {
        try
        {
            await _scheduler.Shutdown(waitForJobsToComplete);
            return Ok(new { message = "Scheduler shutdown initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down scheduler");
            return StatusCode(500, new { error = "Failed to shutdown scheduler" });
        }
    }
}

/// <summary>
/// Request model for creating a scheduled job.
/// </summary>
public class CreateJobRequest
{
    /// <summary>
    /// Name of the job.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Group the job belongs to.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Schedule expression (cron or simple format).
    /// </summary>
    public string Schedule { get; set; } = string.Empty;

    /// <summary>
    /// Type of event to fire.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional end date/time for the schedule.
    /// </summary>
    public DateTime? EndAfter { get; set; }

    /// <summary>
    /// Job content/payload.
    /// </summary>
    public JObject? Content { get; set; }

    /// <summary>
    /// Associated work set ID.
    /// </summary>
    public Guid? HostWorkSet { get; set; }

    /// <summary>
    /// Associated work item ID.
    /// </summary>
    public Guid? HostWorkItem { get; set; }

    /// <summary>
    /// Attached item reference.
    /// </summary>
    public string? AttachedItem { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string>? Tags { get; set; }
}