using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSet: IAppEntity
{
    #region IAppEntity
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public string EntityType { get; set; } = nameof(WorkSet);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
       
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> AttachedSchedules { get; set; } = new();

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);

   

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return WorkSetReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }
    #endregion


    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public Guid? ProjectOwner { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public WorkSetInteractivityState InteractivityState { get; set; }
    public DateTime LockedDate { get; set; } = DateTime.MaxValue; // Report / KPI views shouldn't go beyond this date

    



}
