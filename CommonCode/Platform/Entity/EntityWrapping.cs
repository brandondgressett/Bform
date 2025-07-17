using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

namespace BFormDomain.CommonCode.Platform.Entity;

public class EntityWrapping<T> : IAppEntity
{
    public T? Payload { get; set; }

    public string EntityType { get; set; } = typeof(T).Name;

    public string Template { get; set; } = typeof(T).Name;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public List<string> Tags { get; set; } = new();
    public List<string> AttachedSchedules { get; set; } = new();

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        throw new NotImplementedException();
    }

    public bool Tagged(params string[] anyTags)
    {
        return Tags.Any(t => anyTags.Contains(t));
    }

    public JObject ToJson() => JObject.FromObject(this);

    
}
