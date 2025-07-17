using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;

public class HtmlInstance : IAppEntity
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public string EntityType { get; set; } = nameof(HtmlInstance);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
    public List<string> AttachedSchedules { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return HtmlEntityReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }

    public string Content { get; set; } = null!;

    

}
