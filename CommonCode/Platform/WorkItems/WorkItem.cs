using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;


public class WorkItem: IAppEntity
{
    #region IAppEntity
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string? TenantId { get; set; }

    public string EntityType { get; set; } = nameof(WorkItem);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
       
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> AttachedSchedules { get; set; } = new();
        

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return WorkItemReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);
    #endregion

    

    public bool IsListed { get; set; }

    public bool IsVisible { get; set; }

    public Guid? UserAssignee { get; set; }
    public int? TriageAssignee { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Status { get; set; }
    
    public int Priority { get; set; }

    public DateTime StartUnresolved { get; set; }

    public List<WorkItemEventHistory> EventHistory { get; set; } = new();
    public List<WorkItemBookmark> Bookmarks { get; set; } = new(); 

    public List<WorkItemLink> Links { get; set; } = new();

    public List<Section> Sections { get; set; } = new();

    

    // comments and files are associated entities.
}
