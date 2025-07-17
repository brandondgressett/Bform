using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.DataModels;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Entity;

public interface IAppEntity: IDataModel, ITaggable
{
    /// <summary>
    /// The tenant ID for multi-tenancy support. All entities belong to a specific tenant
    /// except for system-level entities which may have null TenantId.
    /// </summary>
    string? TenantId { get; set; }
  
    string EntityType { get; set; }
    string Template { get; set; }
    DateTime CreatedDate { get; set; }
    DateTime UpdatedDate { get; set; }
    Guid? Creator { get; set; }
    Guid? LastModifier { get; set; }

    Guid? HostWorkSet { get; set; }
    Guid? HostWorkItem { get; set; }

    List<string> AttachedSchedules { get; set; }
   
    bool Tagged(params string[] anyTags);

    JObject ToJson();


    Uri MakeReference(bool template = false, bool vm = false, string? queryParameters=null);
}
