using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.DataModels;
using BFormDomain.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Base;

/// <summary>
/// Base repository for external data sources (RSS feeds, REST APIs, etc).
/// Implements IRepository interface to provide a consistent API while adapting external data.
/// Includes polling mechanisms and change detection with event generation.
/// </summary>
/// <typeparam name="T">The entity type, must implement IDataModel and IAppEntity</typeparam>
public abstract class ExternalDataSourceRepository<T> : IRepository<T>, IHostedService, IDisposable
    where T : class, IDataModel, IAppEntity, new()
{
    private readonly IRepository<AppEvent> _eventRepository;
    private readonly TenantAwareEventFactory _eventFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ExternalDataSourceRepository<T>>? _logger;
    private readonly RepositoryEventConfiguration _eventConfig;
    private Timer? _pollingTimer;
    private readonly TimeSpan _pollingInterval;
    private readonly Dictionary<Guid, T> _cache = new();
    private readonly object _cacheLock = new();
    private bool _isInitialized = false;

    protected ExternalDataSourceRepository(
        IRepository<AppEvent> eventRepository,
        TenantAwareEventFactory eventFactory,
        ITenantContext tenantContext,
        ILogger<ExternalDataSourceRepository<T>>? logger = null,
        TimeSpan? pollingInterval = null)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger;
        _pollingInterval = pollingInterval ?? TimeSpan.FromMinutes(5);
        _eventConfig = CreateEventConfiguration();
    }

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Fetches data from the external source.
    /// </summary>
    protected abstract Task<IEnumerable<T>> FetchDataFromSourceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps external data to the entity model.
    /// </summary>
    protected abstract T MapExternalDataToEntity(object externalData);

    /// <summary>
    /// Determines if the external source supports write operations.
    /// </summary>
    protected abstract bool SupportsWriteOperations { get; }

    /// <summary>
    /// Creates an entity in the external source (if supported).
    /// </summary>
    protected abstract Task<T> CreateInExternalSourceAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity in the external source (if supported).
    /// </summary>
    protected abstract Task<T> UpdateInExternalSourceAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity from the external source (if supported).
    /// </summary>
    protected abstract Task DeleteFromExternalSourceAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Configuration

    protected virtual RepositoryEventConfiguration CreateEventConfiguration()
    {
        return new RepositoryEventConfiguration
        {
            GenerateCreateEvents = true,
            GenerateUpdateEvents = true,
            GenerateDeleteEvents = true,
            CreateEventTopic = $"{typeof(T).Name}.ExternalSource.Created",
            UpdateEventTopic = $"{typeof(T).Name}.ExternalSource.Updated",
            DeleteEventTopic = $"{typeof(T).Name}.ExternalSource.Deleted",
            CreateEventAction = "ExternalCreate",
            UpdateEventAction = "ExternalUpdate",
            DeleteEventAction = "ExternalDelete"
        };
    }

    #endregion

    #region IHostedService Implementation

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting external data source repository for {EntityType}", typeof(T).Name);
        
        // Initial data sync
        _ = Task.Run(async () => await SyncDataAsync(cancellationToken), cancellationToken);
        
        // Start polling timer
        _pollingTimer = new Timer(
            async _ => await SyncDataAsync(),
            null,
            _pollingInterval,
            _pollingInterval);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping external data source repository for {EntityType}", typeof(T).Name);
        _pollingTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion

    #region Data Synchronization

    /// <summary>
    /// Synchronizes data from the external source, detecting changes and generating events.
    /// </summary>
    protected virtual async Task SyncDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Starting data sync for {EntityType}", typeof(T).Name);
            
            var externalData = await FetchDataFromSourceAsync(cancellationToken);
            var externalDataList = externalData.ToList();
            
            lock (_cacheLock)
            {
                // Detect new and updated items
                var processedIds = new HashSet<Guid>();
                
                foreach (var item in externalDataList)
                {
                    processedIds.Add(item.Id);
                    
                    if (_cache.TryGetValue(item.Id, out var existingItem))
                    {
                        // Check if item was updated
                        if (HasChanged(existingItem, item))
                        {
                            _cache[item.Id] = item;
                            _ = GenerateUpdateEventAsync(item, existingItem);
                        }
                    }
                    else
                    {
                        // New item
                        _cache[item.Id] = item;
                        _ = GenerateCreateEventAsync(item);
                    }
                }
                
                // Detect deleted items
                var deletedIds = _cache.Keys.Where(id => !processedIds.Contains(id)).ToList();
                foreach (var deletedId in deletedIds)
                {
                    if (_cache.TryGetValue(deletedId, out var deletedItem))
                    {
                        _cache.Remove(deletedId);
                        _ = GenerateDeleteEventAsync(deletedItem);
                    }
                }
                
                _isInitialized = true;
            }
            
            _logger?.LogDebug("Data sync completed for {EntityType}. Items: {Count}", typeof(T).Name, externalDataList.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during data sync for {EntityType}", typeof(T).Name);
        }
    }

    /// <summary>
    /// Determines if an entity has changed.
    /// Override to provide custom change detection logic.
    /// </summary>
    protected virtual bool HasChanged(T oldItem, T newItem)
    {
        // Simple version comparison if available
        if (oldItem.Version != newItem.Version)
            return true;
        
        // Override this method to implement custom change detection
        // For example, comparing specific properties or timestamps
        return false;
    }

    #endregion

    #region IRepository Implementation - Read Operations

    public async Task<(T, RepositoryContext)> LoadAsync(Guid id)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(id, out var item))
            {
                return (item, new RepositoryContext());
            }
        }
        
        throw new KeyNotFoundException($"Entity with ID {id} not found");
    }

    public async Task<(List<T>, RepositoryContext)> GetAsync(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            var query = _cache.Values.AsQueryable();
            
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            
            var result = query.Skip(start).Take(count).ToList();
            return (result, new RepositoryContext());
        }
    }

    public async Task<(List<T>, RepositoryContext)> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            var result = _cache.Values.AsQueryable().Where(predicate).ToList();
            return (result, new RepositoryContext());
        }
    }

    public async Task<(T?, RepositoryContext)> GetOneAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            var result = _cache.Values.AsQueryable().FirstOrDefault(predicate);
            return (result, new RepositoryContext());
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            return _cache.ContainsKey(id);
        }
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        lock (_cacheLock)
        {
            return _cache.Count;
        }
    }

    #endregion

    #region IRepository Implementation - Write Operations

    public async Task CreateAsync(T newItem)
    {
        if (!SupportsWriteOperations)
            throw new NotSupportedException("This external data source does not support write operations");
        
        // Create in external source
        var createdItem = await CreateInExternalSourceAsync(newItem);
        
        // Update cache
        lock (_cacheLock)
        {
            _cache[createdItem.Id] = createdItem;
        }
        
        // Generate event
        if (_eventConfig.GenerateCreateEvents)
        {
            await GenerateCreateEventAsync(createdItem);
        }
    }

    public async Task UpdateAsync(T data)
    {
        if (!SupportsWriteOperations)
            throw new NotSupportedException("This external data source does not support write operations");
        
        T? oldState = null;
        lock (_cacheLock)
        {
            _cache.TryGetValue(data.Id, out oldState);
        }
        
        // Update in external source
        var updatedItem = await UpdateInExternalSourceAsync(data);
        
        // Update cache
        lock (_cacheLock)
        {
            _cache[updatedItem.Id] = updatedItem;
        }
        
        // Generate event
        if (_eventConfig.GenerateUpdateEvents)
        {
            await GenerateUpdateEventAsync(updatedItem, oldState);
        }
    }

    public async Task DeleteAsync(T doc)
    {
        if (!SupportsWriteOperations)
            throw new NotSupportedException("This external data source does not support write operations");
        
        // Delete from external source
        await DeleteFromExternalSourceAsync(doc.Id);
        
        // Remove from cache
        lock (_cacheLock)
        {
            _cache.Remove(doc.Id);
        }
        
        // Generate event
        if (_eventConfig.GenerateDeleteEvents)
        {
            await GenerateDeleteEventAsync(doc);
        }
    }

    #endregion

    #region Event Generation

    protected virtual async Task GenerateCreateEventAsync(T entity)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.CreateEventTopic,
            action: _eventConfig.CreateEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        var eventData = new
        {
            Source = "External",
            SourceType = GetExternalSourceType(),
            SyncTime = DateTime.UtcNow
        };
        appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
            Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        
        await _eventRepository.CreateAsync(appEvent);
        
        _logger?.LogDebug("Generated create event for external entity {EntityType} {EntityId}", 
            entity.EntityType, entity.Id);
    }

    protected virtual async Task GenerateUpdateEventAsync(T entity, T? oldState)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.UpdateEventTopic,
            action: _eventConfig.UpdateEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        var eventData = new
        {
            Source = "External",
            SourceType = GetExternalSourceType(),
            SyncTime = DateTime.UtcNow,
            HasPreviousState = oldState != null
        };
        appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
            Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        
        await _eventRepository.CreateAsync(appEvent);
        
        _logger?.LogDebug("Generated update event for external entity {EntityType} {EntityId}", 
            entity.EntityType, entity.Id);
    }

    protected virtual async Task GenerateDeleteEventAsync(T entity)
    {
        var appEvent = _eventFactory.CreateEvent(
            topic: _eventConfig.DeleteEventTopic,
            action: _eventConfig.DeleteEventAction,
            entity: entity,
            userId: _tenantContext.CurrentUser?.Id
        );

        var eventData = new
        {
            Source = "External",
            SourceType = GetExternalSourceType(),
            SyncTime = DateTime.UtcNow,
            DeletedEntity = entity
        };
        appEvent.EntityPayload = MongoDB.Bson.BsonDocument.Parse(
            Newtonsoft.Json.JsonConvert.SerializeObject(eventData));
        
        await _eventRepository.CreateAsync(appEvent);
        
        _logger?.LogDebug("Generated delete event for external entity {EntityType} {EntityId}", 
            entity.EntityType, entity.Id);
    }

    /// <summary>
    /// Gets the type of external source (RSS, API, etc).
    /// Override to provide specific source type.
    /// </summary>
    protected virtual string GetExternalSourceType()
    {
        return "Unknown";
    }

    #endregion

    #region Helper Methods

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await SyncDataAsync();
        }
    }

    /// <summary>
    /// Forces a manual sync of data from the external source.
    /// </summary>
    public async Task ForceSyncAsync(CancellationToken cancellationToken = default)
    {
        await SyncDataAsync(cancellationToken);
    }

    #endregion

    #region Not Implemented Methods

    // These methods are not typically supported by external data sources
    // but are required by IRepository interface

    public Task<ITransactionContext> OpenTransactionAsync(CancellationToken ct = default)
    {
        throw new NotSupportedException("Transactions are not supported for external data sources");
    }

    public ITransactionContext OpenTransaction(CancellationToken ct = default)
    {
        throw new NotSupportedException("Transactions are not supported for external data sources");
    }

    public void Create(T newItem) => CreateAsync(newItem).GetAwaiter().GetResult();
    public void Update(T data) => UpdateAsync(data).GetAwaiter().GetResult();
    public void Delete(T doc) => DeleteAsync(doc).GetAwaiter().GetResult();

    // Batch operations - can be overridden if the external source supports them
    public virtual Task CreateBatchAsync(IEnumerable<T> newItems)
    {
        return Task.WhenAll(newItems.Select(CreateAsync));
    }

    public virtual Task DeleteBatchAsync(IEnumerable<Guid> ids)
    {
        return Task.WhenAll(ids.Select(async id =>
        {
            var (item, _) = await LoadAsync(id);
            await DeleteAsync(item);
        }));
    }

    // Additional IRepository methods with default implementations
    public (List<T>, RepositoryContext) Get(int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
        => GetAsync(start, count, predicate).GetAwaiter().GetResult();

    public (List<T>, RepositoryContext) GetAll(Expression<Func<T, bool>> predicate)
        => GetAllAsync(predicate).GetAwaiter().GetResult();

    // Other required methods would follow similar patterns...
    // For brevity, I'll include stubs that throw NotSupportedException

    public Task<(List<T>, RepositoryContext)> GetAllOrderedAsync<TField>(Expression<Func<T, TField>> orderField, bool descending = false, Expression<Func<T, bool>>? predicate = null)
        => throw new NotSupportedException("Ordered queries not implemented for external data sources");

    public Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(Expression<Func<T, TField>> orderField, bool descending = false, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
        => throw new NotSupportedException("Ordered queries not implemented for external data sources");

    public Task<(List<T>, RepositoryContext)> LoadManyAsync(IEnumerable<Guid> ids)
        => throw new NotSupportedException("Bulk load not implemented for external data sources");

    public Task UpdateIgnoreVersionAsync(T data)
        => UpdateAsync(data);

    public Task UpsertIgnoreVersionAsync(T data)
        => ExistsAsync(data.Id).ContinueWith(t => t.Result ? UpdateAsync(data) : CreateAsync(data));

    public void DeleteFilter(Expression<Func<T, bool>> predicate)
        => throw new NotSupportedException("Filter delete not implemented for external data sources");

    public Task DeleteFilterAsync(Expression<Func<T, bool>> predicate)
        => throw new NotSupportedException("Filter delete not implemented for external data sources");

    // Increment operations
    public Task IncrementOneByIdAsync<TField>(Guid id, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    public Task IncrementOneAsync<TField>(Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    public Task IncrementManyAsync<TField>(Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    // Transaction-based operations
    public void Create(ITransactionContext tc, T newItem)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public Task CreateAsync(ITransactionContext? tc, T newItem)
        => CreateAsync(newItem);

    public Task CreateBatchAsync(ITransactionContext tc, IEnumerable<T> newItems)
        => CreateBatchAsync(newItems);

    public Task CreateBatchAsync(RepositoryContext ctx, IEnumerable<T> newItems)
        => CreateBatchAsync(newItems);

    public Task CreateBatchAsync(RepositoryContext rc, ITransactionContext tc, IEnumerable<T> newItems)
        => CreateBatchAsync(newItems);

    public void Delete(ITransactionContext tc, T doc)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public void Delete((T, RepositoryContext) data) => Delete(data.Item1);

    public Task DeleteAsync(ITransactionContext tc, T doc)
        => DeleteAsync(doc);

    public Task DeleteAsync((T, RepositoryContext) data)
        => DeleteAsync(data.Item1);

    public void Delete(ITransactionContext tc, (T, RepositoryContext) data)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public Task DeleteAsync(ITransactionContext tc, (T, RepositoryContext) data)
        => DeleteAsync(data.Item1);

    public void DeleteFilter(ITransactionContext tc, Expression<Func<T, bool>> predicate)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public Task DeleteFilterAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
        => throw new NotSupportedException("Filter delete not implemented for external data sources");

    public Task DeleteBatchAsync(ITransactionContext tc, IEnumerable<Guid> ids)
        => DeleteBatchAsync(ids);

    // Transaction-based read operations
    public (List<T>, RepositoryContext) Get(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
        => Get(start, count, predicate);

    public (List<T>, RepositoryContext) GetAll(ITransactionContext tc, Expression<Func<T, bool>> predicate)
        => GetAll(predicate);

    public Task<(T?, RepositoryContext)> GetOneAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
        => GetOneAsync(predicate);

    public Task<(List<T>, RepositoryContext)> GetAllAsync(ITransactionContext tc, Expression<Func<T, bool>> predicate)
        => GetAllAsync(predicate);

    public Task<(List<T>, RepositoryContext)> GetAsync(ITransactionContext tc, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
        => GetAsync(start, count, predicate);

    public Task<(List<T>, RepositoryContext)> GetOrderedAsync<TField>(ITransactionContext tc, Expression<Func<T, TField>> orderField, bool descending = false, int start = 0, int count = 100, Expression<Func<T, bool>>? predicate = null)
        => throw new NotSupportedException("Ordered queries not implemented for external data sources");

    public Task<(T, RepositoryContext)> LoadAsync(ITransactionContext tc, Guid id)
        => LoadAsync(id);

    public Task<(List<T>, RepositoryContext)> LoadManyAsync(ITransactionContext tc, IEnumerable<Guid> ids)
        => throw new NotSupportedException("Bulk load not implemented for external data sources");

    // Transaction-based update operations
    public void Update(ITransactionContext tc, T data)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public void Update((T, RepositoryContext) data) => Update(data.Item1);

    public Task UpdateAsync(ITransactionContext tc, T data)
        => UpdateAsync(data);

    public Task UpdateAsync((T, RepositoryContext) data)
        => UpdateAsync(data.Item1);

    public void Update(ITransactionContext tc, (T, RepositoryContext) data)
        => throw new NotSupportedException("Transactions not supported for external data sources");

    public Task UpdateAsync(ITransactionContext tc, (T, RepositoryContext) data)
        => UpdateAsync(data.Item1);

    public Task UpdateIgnoreVersionAsync(ITransactionContext tc, T data)
        => UpdateAsync(data);

    public Task UpdateIgnoreVersionAsync((T, RepositoryContext) data)
        => UpdateAsync(data.Item1);

    public Task UpdateIgnoreVersionAsync(ITransactionContext tc, (T, RepositoryContext) data)
        => UpdateAsync(data.Item1);

    public Task UpsertIgnoreVersionAsync(ITransactionContext tc, T data)
        => UpsertIgnoreVersionAsync(data);

    // Transaction-based increment operations
    public Task IncrementOneByIdAsync<TField>(ITransactionContext tc, Guid id, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    public Task IncrementOneAsync<TField>(ITransactionContext tc, Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    public Task IncrementManyAsync<TField>(ITransactionContext tc, Expression<Func<T, bool>> predicate, Expression<Func<T, TField>> field, TField value)
        => throw new NotSupportedException("Increment operations not supported for external data sources");

    // Pagination methods
    public Task<(List<T>, RepositoryContext)> GetPageAsync(int page = 0, Expression<Func<T, bool>>? predicate = null)
    {
        const int pageSize = 100;
        return GetAsync(page * pageSize, pageSize, predicate);
    }

    public Task<(List<T>, RepositoryContext)> GetPageAsync(ITransactionContext tc, int page = 0, Expression<Func<T, bool>>? predicate = null)
        => GetPageAsync(page, predicate);

    public Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(Expression<Func<T, TField>> orderField, bool descending = false, int page = 0, Expression<Func<T, bool>>? predicate = null)
        => throw new NotSupportedException("Ordered pagination not implemented for external data sources");

    public Task<(List<T>, RepositoryContext)> GetOrderedPageAsync<TField>(ITransactionContext tc, Expression<Func<T, TField>> orderField, bool descending = false, int page = 0, Expression<Func<T, bool>>? predicate = null)
        => throw new NotSupportedException("Ordered pagination not implemented for external data sources");

    // Performance optimized methods
    public Task<List<TProjection>> GetWithProjectionAsync<TProjection>(FilterDefinition<T> filter, Expression<Func<T, TProjection>> projection, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Projection queries not supported for external data sources");

    public Task<List<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Aggregation not supported for external data sources");

    public Task<bool> ExistsAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Filter-based existence check not supported for external data sources");

    public Task<long> CountAsync(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Filter-based count not supported for external data sources");

    public Task<UpdateResult> UpdatePartialAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Partial updates not supported for external data sources");

    public Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Bulk updates not supported for external data sources");

    public Task<CursorPaginationResult<T>> GetWithCursorAsync(CursorPaginationRequest request, FilterDefinition<T>? filter = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Cursor pagination not supported for external data sources");

    public Task<CursorPaginationResult<TProjection>> GetWithCursorAsync<TProjection>(CursorPaginationRequest request, Expression<Func<T, TProjection>> projection, FilterDefinition<T>? filter = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Cursor pagination not supported for external data sources");

    public IAsyncEnumerable<T> StreamWithCursorAsync(FilterDefinition<T>? filter = null, string cursorField = "_id", BFormDomain.CommonCode.Repository.Mongo.SortDirection sortDirection = BFormDomain.CommonCode.Repository.Mongo.SortDirection.Ascending, int batchSize = 100, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming not supported for external data sources");

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _pollingTimer?.Dispose();
    }

    #endregion
}