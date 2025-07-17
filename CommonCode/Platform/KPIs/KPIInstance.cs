using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIInstance : IAppEntity
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public string EntityType { get; set; } = nameof(KPIInstance);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }

    public Guid? SubjectUser { get; set; }
    public Guid? SubjectWorkSet { get; set; }
    public Guid? SubjectWorkItem { get; set; }

    public string EventTopic { get; set; } = null!;

    public List<string> Tags { get; set; } = new List<string>();
    public List<string> AttachedSchedules { get; set; } = new();

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return KPIInstanceReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);

   

}
