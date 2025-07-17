using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.DataModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Entity;

/// <summary>
/// Base class for all application entities that provides common properties
/// including multi-tenancy support.
/// </summary>
public abstract class AppEntityBase : IAppEntity
{
    /// <summary>
    /// The tenant ID for multi-tenancy support. All entities belong to a specific tenant
    /// except for system-level entities which may have null TenantId.
    /// </summary>
    public string? TenantId { get; set; }

    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }

    public List<string> AttachedSchedules { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public virtual JObject ToJson() => JObject.FromObject(this);

    public abstract Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null);
}