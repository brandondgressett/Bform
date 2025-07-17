namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Base;

/// <summary>
/// Configuration for controlling how a repository generates events.
/// Provides fine-grained control over event generation behavior.
/// </summary>
public class RepositoryEventConfiguration
{
    /// <summary>
    /// Whether to generate events for create operations.
    /// </summary>
    public bool GenerateCreateEvents { get; set; } = true;

    /// <summary>
    /// Whether to generate events for update operations.
    /// </summary>
    public bool GenerateUpdateEvents { get; set; } = true;

    /// <summary>
    /// Whether to generate events for delete operations.
    /// </summary>
    public bool GenerateDeleteEvents { get; set; } = true;

    /// <summary>
    /// Whether to generate individual events for batch operations.
    /// If false, generates a single batch event instead.
    /// </summary>
    public bool GenerateBatchEvents { get; set; } = true;

    /// <summary>
    /// Topic name for create events.
    /// </summary>
    public string CreateEventTopic { get; set; } = "Entity.Created";

    /// <summary>
    /// Topic name for update events.
    /// </summary>
    public string UpdateEventTopic { get; set; } = "Entity.Updated";

    /// <summary>
    /// Topic name for delete events.
    /// </summary>
    public string DeleteEventTopic { get; set; } = "Entity.Deleted";

    /// <summary>
    /// Action name for create events.
    /// </summary>
    public string CreateEventAction { get; set; } = "Create";

    /// <summary>
    /// Action name for update events.
    /// </summary>
    public string UpdateEventAction { get; set; } = "Update";

    /// <summary>
    /// Action name for delete events.
    /// </summary>
    public string DeleteEventAction { get; set; } = "Delete";

    /// <summary>
    /// Whether to include the old state of the entity in update events.
    /// Useful for audit trails and change tracking.
    /// </summary>
    public bool IncludeOldStateInEvents { get; set; } = false;

    /// <summary>
    /// Whether to track individual property changes in update events.
    /// Requires IncludeOldStateInEvents to be true.
    /// </summary>
    public bool TrackPropertyChanges { get; set; } = false;

    /// <summary>
    /// Whether to include the full entity data in delete events.
    /// Useful for recovery and audit purposes.
    /// </summary>
    public bool IncludeEntityInDeleteEvent { get; set; } = true;

    /// <summary>
    /// Filter predicate to determine if an event should be generated.
    /// Return true to generate the event, false to skip it.
    /// </summary>
    public Func<object, string, bool>? EventFilter { get; set; }

    /// <summary>
    /// Custom event data provider.
    /// Allows adding custom data to all events.
    /// </summary>
    public Func<object, string, object?>? CustomEventDataProvider { get; set; }

    /// <summary>
    /// Maximum number of events to generate in a single batch.
    /// Helps prevent overwhelming the event system.
    /// </summary>
    public int MaxBatchEventCount { get; set; } = 100;

    /// <summary>
    /// Whether to generate events for system operations.
    /// System operations are those performed by background processes.
    /// </summary>
    public bool GenerateSystemOperationEvents { get; set; } = true;

    /// <summary>
    /// List of property names to exclude from change tracking.
    /// Useful for ignoring system properties like Version, UpdatedDate, etc.
    /// </summary>
    public HashSet<string> ExcludedProperties { get; set; } = new()
    {
        "Version",
        "UpdatedDate",
        "UpdatedBy"
    };

    /// <summary>
    /// Whether events should be generated synchronously or queued for async processing.
    /// </summary>
    public bool GenerateEventsAsync { get; set; } = false;

    /// <summary>
    /// Custom event topics for specific operations.
    /// Key is the operation name, value is the topic.
    /// </summary>
    public Dictionary<string, string> CustomEventTopics { get; set; } = new();

    /// <summary>
    /// Determines if an event should be generated based on the configuration and filters.
    /// </summary>
    public bool ShouldGenerateEvent(object entity, string operation)
    {
        // Check if the operation type is enabled
        var isEnabled = operation.ToLowerInvariant() switch
        {
            "create" => GenerateCreateEvents,
            "update" => GenerateUpdateEvents,
            "delete" => GenerateDeleteEvents,
            _ => true
        };

        if (!isEnabled) return false;

        // Apply custom filter if provided
        if (EventFilter != null)
        {
            return EventFilter(entity, operation);
        }

        return true;
    }

    /// <summary>
    /// Gets the event topic for a specific operation.
    /// </summary>
    public string GetEventTopic(string operation)
    {
        // Check custom topics first
        if (CustomEventTopics.TryGetValue(operation, out var customTopic))
        {
            return customTopic;
        }

        // Return default topics
        return operation.ToLowerInvariant() switch
        {
            "create" => CreateEventTopic,
            "update" => UpdateEventTopic,
            "delete" => DeleteEventTopic,
            _ => $"Entity.{operation}"
        };
    }

    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    public RepositoryEventConfiguration Clone()
    {
        return new RepositoryEventConfiguration
        {
            GenerateCreateEvents = GenerateCreateEvents,
            GenerateUpdateEvents = GenerateUpdateEvents,
            GenerateDeleteEvents = GenerateDeleteEvents,
            GenerateBatchEvents = GenerateBatchEvents,
            CreateEventTopic = CreateEventTopic,
            UpdateEventTopic = UpdateEventTopic,
            DeleteEventTopic = DeleteEventTopic,
            CreateEventAction = CreateEventAction,
            UpdateEventAction = UpdateEventAction,
            DeleteEventAction = DeleteEventAction,
            IncludeOldStateInEvents = IncludeOldStateInEvents,
            TrackPropertyChanges = TrackPropertyChanges,
            IncludeEntityInDeleteEvent = IncludeEntityInDeleteEvent,
            EventFilter = EventFilter,
            CustomEventDataProvider = CustomEventDataProvider,
            MaxBatchEventCount = MaxBatchEventCount,
            GenerateSystemOperationEvents = GenerateSystemOperationEvents,
            ExcludedProperties = new HashSet<string>(ExcludedProperties),
            GenerateEventsAsync = GenerateEventsAsync,
            CustomEventTopics = new Dictionary<string, string>(CustomEventTopics)
        };
    }
}