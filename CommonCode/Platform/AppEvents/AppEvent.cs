using BFormDomain.CommonCode.Utility;
using BFormDomain.DataModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Tenancy;

namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// AppEvent tracks information about events generated anything significant happening for the rules engine to process
///         >AppEventOrigin.cs
///         >AppEventPump.cs
///         >AppEventRepository.cs
///         >AppEventSink.cs
///         >IAppEventConsumer.cs
///         >RuleAction files
///     -Functions:
///         >ToPreceding
///         >ToRuleView
/// </summary>
public class AppEvent : IDataModel, ITenantScoped
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    // Multi-tenancy property
    public Guid TenantId { get; set; }

    public string? Topic { get; set; }

    /// <summary>
    /// Type of entity that generated the event
    /// </summary>
    public string? OriginEntityType { get; set; }

    /// <summary>
    /// Template type of entity that generated the event.
    /// </summary>
    public string? OriginEntityTemplate { get; set; }

    /// <summary>
    /// Id of the entity that generated the event.
    /// </summary>
    public Guid? OriginEntityId { get; set; }
    public string? GeneratorId { get; set; }

    public string? ActionId { get; set; }

    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }

    /// <summary>
    /// If true, a user action produced this event or a preceding
    /// event was because of a user action. 
    /// Otherwise, the system generated this event itself.
    /// </summary>
    public bool IsNatural { get; set; }

    /// <summary>
    /// ApplicationUser who started the action leading to this event.
    /// </summary>
    public Guid? OriginUser { get; set; }

    /// <summary>
    /// Events may cause actions that cause events. 
    /// The event line groups them together from the original
    /// user action.
    /// </summary>
    public Guid EventLine { get; set; }

    /// <summary>
    /// How many event generations from the original user action
    /// that set the dominos tumbling.
    /// </summary>
    public int EventGeneration { get; set; }

    /// <summary>
    /// Time the event was created.
    /// </summary>
    public DateTime Created { get; set; }

    public DateTime DeferredUntil { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Time the event was taken from the repo for
    /// processing.
    /// </summary>
    public DateTime TakenExpiration { get; set; }
    /// <summary>
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public AppEventState State { get; set; }

    /// <summary>
    /// </summary>
    [JsonIgnore]
    public BsonDocument EntityPayload { get; set; } = default!;

    [BsonIgnore]
    public string JsonPayload { get { return EntityPayload.ToJsonString(); } }

    public List<string> Tags { get; set; } = new();
    public List<string> EntityTags { get; set; } = new();

    public void AddTag(string tag)
    {
        if(!Tags.Contains(tag)) Tags.Add(tag);
    }

    public bool Seal { get; set; } = false;

    public int SendRetries { get; set; } = 0;



    public AppEventOrigin ToPreceding(string generator)
    {
        return new AppEventOrigin(generator,this.ActionId, this);
    }

    

    public JObject ToRuleView()
    {
        var result = new AppEventRuleView
        {
            Data = JObject.Parse(EntityPayload.ToJson()),
            Topic = this.Topic!,
            ActionUser = OriginUser,
            EventTags = Tags,
            HostWorkSet = HostWorkSet,
            HostWorkItem = HostWorkItem,
            EntityTemplate = OriginEntityTemplate,
            EntityType = OriginEntityType,
            EntityId = OriginEntityId,
            EntityTags = EntityTags
        };

        return JObject.FromObject(result);
    }
}
