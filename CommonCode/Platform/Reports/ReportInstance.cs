using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Reports;

public class ReportInstance : IAppEntity
{
    public string EntityType { get; set; } = nameof(ReportInstance);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
   
   

    public List<string> AttachedSchedules { get; set; } = new();

    public string Html { get; set; } = null!;

    public DateTime? GroomDate { get; set; }

    public string Title { get; set; } = "";


    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public List<string> Tags { get; set; } = new();

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return ReportInstanceReferenceBuilderImplementation.MakeReference(
            Template, Id, template, vm, queryParameters);
    }

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);

    
}
