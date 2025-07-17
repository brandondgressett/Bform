using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.DataModels;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Base;

/// <summary>
/// Base repository class that automatically generates AppEvents for all data operations.
/// Extends MongoRepository to provide event-aware CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type, must implement IDataModel</typeparam>
public abstract class EventAwareRepository<T> : TenantAwareMongoRepository<T> 
    where T : class, IDataModel, IAppEntity
{
    private readonly IRepository<AppEvent> _eventRepository;
    private readonly TenantAwareEventFactory _eventFactory;
    private readonly new ITenantContext _tenantContext;
    private readonly new ILogger<EventAwareRepository<T>>? _logger;
    private readonly RepositoryEventConfiguration _eventConfig;

    protected EventAwareRepository(
        ITenantContext tenantContext,
        ITenantConnectionProvider connectionProvider,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        IRepository<AppEvent> eventRepository,
        TenantAwareEventFactory eventFactory,
        SimpleApplicationAlert alerts,
        ILogger<EventAwareRepository<T>>? logger = null,
        TenantConnectionPool? connectionPool = null)
        : base(tenantContext, connectionProvider, multiTenancyOptions, alerts, logger, connectionPool)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger;
        _eventConfig = CreateEventConfiguration();
    }

    /// <summary>
    /// Override this to provide custom event configuration for the repository.
    /// </summary>
    protected virtual RepositoryEventConfiguration CreateEventConfiguration()
    {
        return new RepositoryEventConfiguration
        {
            GenerateCreateEvents = true,
            GenerateUpdateEvents = true,
            GenerateDeleteEvents = true,
            CreateEventTopic = $"{typeof(T).Name}.Created",
            UpdateEventTopic = $"{typeof(T).Name}.Updated",
            DeleteEventTopic = $"{typeof(T).Name}.Deleted",
            CreateEventAction = "Create",
            UpdateEventAction = "Update",
            DeleteEventAction = "Delete"
        };
    }

    #region Create Operations with Events

    public override async Task CreateAsync(T newItem)
    {
        await base.CreateAsync(newItem);
        
        if (_eventConfig.GenerateCreateEvents)
        {
            await GenerateCreateEventAsync(newItem);
        }
    }

    public override async Task CreateAsync(ITransactionContext? tc, T newItem)
    {
        await base.CreateAsync(tc, newItem);
        
        if (_eventConfig.GenerateCreateEvents)
        {
            await GenerateCreateEventAsync(newItem, tc);
        }
    }

    public override async Task CreateBatchAsync(IEnumerable<T> newItems)
    {
        var itemsList = newItems.ToList();
        await base.CreateBatchAsync(itemsList);
        
        if (_eventConfig.GenerateCreateEvents && _eventConfig.GenerateBatchEvents)
        {
            foreach (var item in itemsList)
            {
                await GenerateCreateEventAsync(item);
            }
        }
    }

    #endregion

    #region Update Operations with Events

    public override async Task UpdateAsync(T data)
    {
        // Capture old state if needed for event
        T? oldState = null;
        if (_eventConfig.IncludeOldStateInEvents)
        {
            var (existing, _) = await LoadAsync(data.Id);
            oldState = existing;
        }

        await base.UpdateAsync(data);
        
        if (_eventConfig.GenerateUpdateEvents)
        {
            await GenerateUpdateEventAsync(data, oldState);
        }
    }

    public override async Task UpdateAsync(ITransactionContext tc, T data)
    {
        T? oldState = null;
        if (_eventConfig.IncludeOldStateInEvents)
        {
            var (existing, _) = await LoadAsync(tc, data.Id);
            oldState = existing;
        }

        await base.UpdateAsync(tc, data);
        
        if (_eventConfig.GenerateUpdateEvents)
        {
            await GenerateUpdateEventAsync(data, oldState, tc);
        }
    }

    #endregion

    #region Delete Operations with Events

    public override async Task DeleteAsync(T doc)
    {
        // Capture state before deletion for event
        var deletedItem = doc;
        
        await base.DeleteAsync(doc);
        
        if (_eventConfig.GenerateDeleteEvents)
        {
            await GenerateDeleteEventAsync(deletedItem);
        }
    }

    public override async Task DeleteAsync(ITransactionContext tc, T doc)
    {
        var deletedItem = doc;
        
        await base.DeleteAsync(tc, doc);
        
        if (_eventConfig.GenerateDeleteEvents)
        {
            await GenerateDeleteEventAsync(deletedItem, tc);
        }
    }

    public override async Task DeleteFilterAsync(Expression<Func<T, bool>> predicate)
    {
        // Get items to be deleted if events are needed
        List<T>? itemsToDelete = null;
        if (_eventConfig.GenerateDeleteEvents)
        {
            var (items, _) = await GetAllAsync(predicate);
            itemsToDelete = items;
        }

        await base.DeleteFilterAsync(predicate);
        
        if (_eventConfig.GenerateDeleteEvents && itemsToDelete != null)
        {
            foreach (var item in itemsToDelete)
            {
                await GenerateDeleteEventAsync(item);
            }
        }
    }

    #endregion

    #region Event Generation Methods

    /// <summary>
    /// Generates an event for entity creation.
    /// Override to customize event generation.
    /// </summary>
    protected virtual async Task GenerateCreateEventAsync(T entity, ITransactionContext? tc = null)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.CreateEventTopic,
            action: _eventConfig.CreateEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        // Add custom event data
        var eventData = await GetCreateEventDataAsync(entity);
        if (eventData != null)
        {
            appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        }
        
        _logger?.LogDebug(
            "Generated create event for {EntityType} {EntityId}",
            entity.EntityType, entity.Id);

        if (tc != null)
        {
            await _eventRepository.CreateAsync(tc, appEvent);
        }
        else
        {
            await _eventRepository.CreateAsync(appEvent);
        }
    }

    /// <summary>
    /// Generates an event for entity update.
    /// Override to customize event generation.
    /// </summary>
    protected virtual async Task GenerateUpdateEventAsync(T entity, T? oldState, ITransactionContext? tc = null)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.UpdateEventTopic,
            action: _eventConfig.UpdateEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        // Add custom event data including changes
        var eventData = await GetUpdateEventDataAsync(entity, oldState);
        if (eventData != null)
        {
            appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        }
        
        _logger?.LogDebug(
            "Generated update event for {EntityType} {EntityId}",
            entity.EntityType, entity.Id);

        if (tc != null)
        {
            await _eventRepository.CreateAsync(tc, appEvent);
        }
        else
        {
            await _eventRepository.CreateAsync(appEvent);
        }
    }

    /// <summary>
    /// Generates an event for entity deletion.
    /// Override to customize event generation.
    /// </summary>
    protected virtual async Task GenerateDeleteEventAsync(T entity, ITransactionContext? tc = null)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.DeleteEventTopic,
            action: _eventConfig.DeleteEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        // Add custom event data
        var eventData = await GetDeleteEventDataAsync(entity);
        if (eventData != null)
        {
            appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        }
        
        _logger?.LogDebug(
            "Generated delete event for {EntityType} {EntityId}",
            entity.EntityType, entity.Id);

        if (tc != null)
        {
            await _eventRepository.CreateAsync(tc, appEvent);
        }
        else
        {
            await _eventRepository.CreateAsync(appEvent);
        }
    }

    #endregion

    #region Event Data Methods

    /// <summary>
    /// Override to provide custom data for create events.
    /// </summary>
    protected virtual Task<object?> GetCreateEventDataAsync(T entity)
    {
        if (entity is IEventAwareEntity eventAware)
        {
            return Task.FromResult(eventAware.GetEventData("create"));
        }
        
        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// Override to provide custom data for update events.
    /// Includes change tracking if old state is provided.
    /// </summary>
    protected virtual Task<object?> GetUpdateEventDataAsync(T entity, T? oldState)
    {
        var eventData = new Dictionary<string, object?>();
        
        if (entity is IEventAwareEntity eventAware)
        {
            eventData["custom"] = eventAware.GetEventData("update");
        }
        
        if (oldState != null && _eventConfig.TrackPropertyChanges)
        {
            eventData["changes"] = GetPropertyChanges(oldState, entity);
        }
        
        return Task.FromResult<object?>(eventData.Any() ? eventData : null);
    }

    /// <summary>
    /// Override to provide custom data for delete events.
    /// </summary>
    protected virtual Task<object?> GetDeleteEventDataAsync(T entity)
    {
        if (entity is IEventAwareEntity eventAware)
        {
            return Task.FromResult(eventAware.GetEventData("delete"));
        }
        
        // Include full entity data in delete event for audit purposes
        if (_eventConfig.IncludeEntityInDeleteEvent)
        {
            return Task.FromResult<object?>(entity);
        }
        
        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// Compares two entities and returns the properties that changed.
    /// </summary>
    protected virtual Dictionary<string, object?> GetPropertyChanges(T oldState, T newState)
    {
        var changes = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var oldValue = property.GetValue(oldState);
            var newValue = property.GetValue(newState);
            
            if (!Equals(oldValue, newValue))
            {
                changes[property.Name] = new
                {
                    OldValue = oldValue,
                    NewValue = newValue
                };
            }
        }

        return changes;
    }

    #endregion

    #region Bulk Event Operations

    /// <summary>
    /// Generates a single batch event for multiple operations.
    /// Used when GenerateBatchEvents is false but individual events are enabled.
    /// </summary>
    protected virtual async Task GenerateBatchEventAsync(
        string topic, 
        string action, 
        IEnumerable<T> entities,
        ITransactionContext? tc = null)
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any()) return;

        // Use first entity as the primary entity for the event
        var primaryEntity = entitiesList.First();
        var appEvent = _eventFactory.CreateEvent(
            topic: topic,
            action: action,
            entity: primaryEntity,
            userId: _tenantContext.CurrentUser?.Id
        );

        // Include all entity IDs in event data
        var batchEventData = new
        {
            EntityCount = entitiesList.Count,
            EntityIds = entitiesList.Select(e => e.Id).ToList(),
            BatchOperation = true
        };
        appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
            Newtonsoft.Json.JsonConvert.SerializeObject(batchEventData));

        _logger?.LogDebug(
            "Generated batch event for {Count} {EntityType} entities",
            entitiesList.Count, primaryEntity.EntityType);

        if (tc != null)
        {
            await _eventRepository.CreateAsync(tc, appEvent);
        }
        else
        {
            await _eventRepository.CreateAsync(appEvent);
        }
    }

    #endregion
}