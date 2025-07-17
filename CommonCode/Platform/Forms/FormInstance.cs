using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Forms;

public class FormInstance : IAppEntity
{
    /// <summary>
    /// The tenant ID for multi-tenancy support.
    /// </summary>
    public string? TenantId { get; set; }
    
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public string EntityType { get; set; } = nameof(FormInstance);
    public string Template { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid? Creator { get; set; }
    public Guid? LastModifier { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }

    [BsonRepresentation(BsonType.String)]
    [JsonConverter(typeof(StringEnumConverter))]
    public FormInstanceHome Home { get; set; }
    
    public List<string> Tags { get; set; } = new List<string>();

    public List<string> AttachedSchedules { get; set; } = new();

    [JsonIgnore]
    [JsonConverter(typeof(BsonToJsonConverter))]
    public BsonDocument? Content { get; set; }

    [BsonIgnore]
    public JObject? JsonContent { get; set; }
    //public JObject JsonContent { get { return JObject.Parse(Content!.ToJsonString()); } set { Content = value.ToBsonObject(); } }

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return FormInstanceReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }

    public bool Tagged(params string[] anyTags) => Tags.Any(t => anyTags.Contains(t));

    public JObject ToJson() => JObject.FromObject(this);
}
