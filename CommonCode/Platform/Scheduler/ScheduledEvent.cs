using BFormDomain.CommonCode.Platform.Entity;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Scheduler;

public class ScheduledEvent : IAppEntity
{
    /// <summary>
    /// The tenant ID for this scheduled event. Ensures scheduled events are isolated per tenant.
    /// </summary>
    public string? TenantId { get; set; }
    
    public string EntityType { get; set; } = nameof(ScheduledEvent);
    public string Template { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }


    public string EventTopic { get; set; } = null!;
    public string Schedule { get; set; } = null!;

    public string? AttachedTo { get; set; }

    public JObject? Content { get; set; }

    public List<string> Tags { get; set; } = new();



    public List<string> AttachedSchedules { get; set; } = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; }

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        throw new NotImplementedException();
    }

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);

}
