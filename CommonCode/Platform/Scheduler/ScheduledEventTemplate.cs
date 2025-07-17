using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Validation;

namespace BFormDomain.CommonCode.Platform.Scheduler;

public class ScheduledEventTemplate : IContentType
{
    public string Name { get; set; } = null!;
    public int DescendingOrder { get; set; }
    public string? DomainName { get { return nameof(ScheduledEventTemplate); } set { } }

    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// The last part of the event topic for event matching.
    /// </summary>
    public string EventTopic { get; set; } = null!;

    /// <summary>
    /// prefixed with:
    ///     "ts:" a timespan acceptible to TimeSpan.Parse(). Will not repeat. (see https://docs.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-6.0)
    ///     "rf:" a timespan acceptible to TimeSpan.Parse(). Will repeat forever until descheduled.
    ///     "rc:" Eg: "rp:{timespan}|{repeat count}" Repeat count and timespan separated by bar ("|")
    ///     "cr:" a cron expression. See https://www.freeformatter.com/cron-expression-generator-quartz.html
    /// </summary>
    public string Schedule { get; set; } = null!;

    /// <summary>
    /// Optional group name for the scheduled job
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Optional end date/time after which the schedule should not execute
    /// </summary>
    public DateTime? EndAfter { get; set; }

    public ScheduledEventIdentifier CreateIdentifier(Guid schId)
    {
        var group = $"{Name}.{EventTopic}";
        var id = GuidEncoder.Encode(schId)!;
        return new ScheduledEventIdentifier(nameof(ScheduledEvent), id, group, id);
    }
    
    public string CreateEventTopic(string wsTemplateName, string wiTemplateName)
    {
        EventTopic.Guarantees().IsNotNullOrEmpty();
        return $"{wsTemplateName}.{wiTemplateName}.scheduled.event.{EventTopic.ToLowerInvariant()}";
    }

}
