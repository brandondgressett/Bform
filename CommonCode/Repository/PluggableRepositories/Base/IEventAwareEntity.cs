using BFormDomain.DataModels;
using BFormDomain.CommonCode.Platform.Entity;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Base;

/// <summary>
/// Interface for entities that can provide custom event data and metadata.
/// Extends IDataModel to add event-specific capabilities.
/// </summary>
public interface IEventAwareEntity : IDataModel, IAppEntity
{
    /// <summary>
    /// Gets custom event data for a specific operation.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>Custom data to include in the event, or null if no custom data</returns>
    object? GetEventData(string operation);

    /// <summary>
    /// Gets custom event metadata for a specific operation.
    /// Metadata is typically used for routing or filtering events.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>Dictionary of metadata key-value pairs</returns>
    Dictionary<string, string>? GetEventMetadata(string operation);

    /// <summary>
    /// Determines if an event should be generated for this entity and operation.
    /// Allows entity-level control over event generation.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>True if event should be generated, false otherwise</returns>
    bool ShouldGenerateEvent(string operation);

    /// <summary>
    /// Gets a custom event topic for this entity and operation.
    /// Allows entities to override default event topics.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>Custom event topic, or null to use default</returns>
    string? GetCustomEventTopic(string operation);

    /// <summary>
    /// Gets a custom event action for this entity and operation.
    /// Allows entities to override default event actions.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>Custom event action, or null to use default</returns>
    string? GetCustomEventAction(string operation);

    /// <summary>
    /// Gets priority for the event.
    /// Higher priority events may be processed first.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>Event priority (0-100, where 100 is highest)</returns>
    int GetEventPriority(string operation);

    /// <summary>
    /// Gets tags to add to the generated event.
    /// Tags can be used for filtering and routing.
    /// </summary>
    /// <param name="operation">The operation type (create, update, delete)</param>
    /// <returns>List of tags to add to the event</returns>
    List<string> GetEventTags(string operation);
}

/// <summary>
/// Base implementation of IEventAwareEntity providing default behavior.
/// Entities can extend this class for convenience.
/// </summary>
public abstract class EventAwareEntityBase : IEventAwareEntity
{
    // IDataModel implementation
    public Guid Id { get; set; }
    public int Version { get; set; }

    // IAppEntity implementation
    public string? TenantId { get; set; }
    public abstract string EntityType { get; set; }
    public virtual string Template { get; set; } = string.Empty;
    public virtual DateTime CreatedDate { get; set; }
    public virtual DateTime UpdatedDate { get; set; }
    public virtual Guid? Creator { get; set; }
    public virtual Guid? LastModifier { get; set; }
    public virtual List<string> Tags { get; set; } = new();
    public virtual Guid? HostWorkSet { get; set; }
    public virtual Guid? HostWorkItem { get; set; }
    public virtual List<string> AttachedSchedules { get; set; } = new();

    /// <summary>
    /// Default implementation returns null (no custom data).
    /// Override to provide custom event data.
    /// </summary>
    public virtual object? GetEventData(string operation)
    {
        return null;
    }

    /// <summary>
    /// Default implementation returns null (no custom metadata).
    /// Override to provide custom event metadata.
    /// </summary>
    public virtual Dictionary<string, string>? GetEventMetadata(string operation)
    {
        return null;
    }

    /// <summary>
    /// Default implementation returns true (generate all events).
    /// Override to control event generation.
    /// </summary>
    public virtual bool ShouldGenerateEvent(string operation)
    {
        return true;
    }

    /// <summary>
    /// Default implementation returns null (use default topic).
    /// Override to provide custom event topics.
    /// </summary>
    public virtual string? GetCustomEventTopic(string operation)
    {
        return null;
    }

    /// <summary>
    /// Default implementation returns null (use default action).
    /// Override to provide custom event actions.
    /// </summary>
    public virtual string? GetCustomEventAction(string operation)
    {
        return null;
    }

    /// <summary>
    /// Default implementation returns medium priority (50).
    /// Override to set custom priorities.
    /// </summary>
    public virtual int GetEventPriority(string operation)
    {
        return 50;
    }

    /// <summary>
    /// Default implementation returns entity tags.
    /// Override to add operation-specific tags.
    /// </summary>
    public virtual List<string> GetEventTags(string operation)
    {
        return Tags ?? new List<string>();
    }

    /// <summary>
    /// Default implementation checks if entity has any of the specified tags.
    /// </summary>
    public virtual bool Tagged(params string[] anyTags)
    {
        if (anyTags == null || anyTags.Length == 0) return false;
        return Tags?.Any(tag => anyTags.Contains(tag)) ?? false;
    }

    /// <summary>
    /// Default implementation converts entity to JSON.
    /// </summary>
    public virtual Newtonsoft.Json.Linq.JObject ToJson()
    {
        return Newtonsoft.Json.Linq.JObject.FromObject(this);
    }

    /// <summary>
    /// Default implementation creates a reference URI for the entity.
    /// </summary>
    public virtual Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        var path = template ? $"/template/{EntityType}" : $"/entity/{EntityType}/{Id}";
        if (vm) path += "/vm";
        if (!string.IsNullOrEmpty(queryParameters)) path += $"?{queryParameters}";
        return new Uri(path, UriKind.Relative);
    }
}

/// <summary>
/// Extension methods for working with event-aware entities.
/// </summary>
public static class EventAwareEntityExtensions
{
    /// <summary>
    /// Safely gets event data from an entity, handling non-event-aware entities.
    /// </summary>
    public static object? SafeGetEventData(this IAppEntity entity, string operation)
    {
        return entity is IEventAwareEntity eventAware 
            ? eventAware.GetEventData(operation) 
            : null;
    }

    /// <summary>
    /// Safely checks if an entity should generate an event.
    /// </summary>
    public static bool SafeShouldGenerateEvent(this IAppEntity entity, string operation)
    {
        return entity is IEventAwareEntity eventAware 
            ? eventAware.ShouldGenerateEvent(operation) 
            : true;
    }

    /// <summary>
    /// Creates a dictionary of all event-related information for an entity.
    /// </summary>
    public static Dictionary<string, object?> GetEventInfo(this IEventAwareEntity entity, string operation)
    {
        return new Dictionary<string, object?>
        {
            ["EntityType"] = entity.EntityType,
            ["EntityId"] = entity.Id,
            ["Operation"] = operation,
            ["CustomData"] = entity.GetEventData(operation),
            ["Metadata"] = entity.GetEventMetadata(operation),
            ["CustomTopic"] = entity.GetCustomEventTopic(operation),
            ["CustomAction"] = entity.GetCustomEventAction(operation),
            ["Priority"] = entity.GetEventPriority(operation),
            ["Tags"] = entity.GetEventTags(operation)
        };
    }
}