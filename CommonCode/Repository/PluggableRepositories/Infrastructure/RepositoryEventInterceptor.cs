using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Repository.PluggableRepositories.Base;
using BFormDomain.DataModels;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Infrastructure;

/// <summary>
/// Decorator that wraps repository operations to provide cross-cutting concerns.
/// Can be used to add logging, validation, caching, or event generation to any repository.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class RepositoryEventInterceptor<T> 
    where T : class, IDataModel, IAppEntity
{
    private readonly IRepository<T> _innerRepository;
    private readonly IRepository<AppEvent>? _eventRepository;
    private readonly ILogger<RepositoryEventInterceptor<T>>? _logger;
    private readonly List<IRepositoryInterceptor<T>> _interceptors;

    public RepositoryEventInterceptor(
        IRepository<T> innerRepository,
        IRepository<AppEvent>? eventRepository = null,
        ILogger<RepositoryEventInterceptor<T>>? logger = null,
        IEnumerable<IRepositoryInterceptor<T>>? interceptors = null)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _eventRepository = eventRepository;
        _logger = logger;
        _interceptors = interceptors?.ToList() ?? new List<IRepositoryInterceptor<T>>();
    }

    /// <summary>
    /// Adds an interceptor to the chain.
    /// </summary>
    public void AddInterceptor(IRepositoryInterceptor<T> interceptor)
    {
        _interceptors.Add(interceptor);
    }

    /// <summary>
    /// Gets the wrapped repository for direct access if needed.
    /// </summary>
    public IRepository<T> InnerRepository => _innerRepository;

    #region Intercepted Create Operations

    public async Task CreateAsync(T newItem)
    {
        // Pre-operation interceptors
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeCreateAsync(newItem);
        }

        try
        {
            await _innerRepository.CreateAsync(newItem);
            
            // Post-operation interceptors
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterCreateAsync(newItem);
            }
        }
        catch (Exception ex)
        {
            // Error interceptors
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnCreateErrorAsync(newItem, ex);
            }
            throw;
        }
    }

    public async Task CreateAsync(ITransactionContext? tc, T newItem)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeCreateAsync(newItem, tc);
        }

        try
        {
            await _innerRepository.CreateAsync(tc, newItem);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterCreateAsync(newItem, tc);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnCreateErrorAsync(newItem, ex, tc);
            }
            throw;
        }
    }

    public async Task CreateBatchAsync(IEnumerable<T> newItems)
    {
        var itemsList = newItems.ToList();
        
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeCreateBatchAsync(itemsList);
        }

        try
        {
            await _innerRepository.CreateBatchAsync(itemsList);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterCreateBatchAsync(itemsList);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnCreateBatchErrorAsync(itemsList, ex);
            }
            throw;
        }
    }

    #endregion

    #region Intercepted Update Operations

    public async Task UpdateAsync(T data)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeUpdateAsync(data);
        }

        try
        {
            await _innerRepository.UpdateAsync(data);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterUpdateAsync(data);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnUpdateErrorAsync(data, ex);
            }
            throw;
        }
    }

    public async Task UpdateAsync(ITransactionContext tc, T data)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeUpdateAsync(data, tc);
        }

        try
        {
            await _innerRepository.UpdateAsync(tc, data);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterUpdateAsync(data, tc);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnUpdateErrorAsync(data, ex, tc);
            }
            throw;
        }
    }

    #endregion

    #region Intercepted Delete Operations

    public async Task DeleteAsync(T doc)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeDeleteAsync(doc);
        }

        try
        {
            await _innerRepository.DeleteAsync(doc);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterDeleteAsync(doc);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnDeleteErrorAsync(doc, ex);
            }
            throw;
        }
    }

    public async Task DeleteAsync(ITransactionContext tc, T doc)
    {
        foreach (var interceptor in _interceptors)
        {
            await interceptor.OnBeforeDeleteAsync(doc, tc);
        }

        try
        {
            await _innerRepository.DeleteAsync(tc, doc);
            
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnAfterDeleteAsync(doc, tc);
            }
        }
        catch (Exception ex)
        {
            foreach (var interceptor in _interceptors)
            {
                await interceptor.OnDeleteErrorAsync(doc, ex, tc);
            }
            throw;
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Creates a new interceptor with the specified interceptor added.
    /// </summary>
    public static RepositoryEventInterceptor<T> Create(
        IRepository<T> repository,
        params IRepositoryInterceptor<T>[] interceptors)
    {
        return new RepositoryEventInterceptor<T>(repository, null, null, interceptors);
    }

    /// <summary>
    /// Adds logging to all repository operations.
    /// </summary>
    public RepositoryEventInterceptor<T> WithLogging(ILogger<LoggingInterceptor<T>> logger)
    {
        AddInterceptor(new LoggingInterceptor<T>(logger));
        return this;
    }

    /// <summary>
    /// Adds validation to create and update operations.
    /// </summary>
    public RepositoryEventInterceptor<T> WithValidation(IValidator<T>? validator = null)
    {
        AddInterceptor(new ValidationInterceptor<T>(validator));
        return this;
    }

    /// <summary>
    /// Adds event generation to all operations.
    /// </summary>
    public RepositoryEventInterceptor<T> WithEventGeneration(
        IRepository<AppEvent> eventRepository,
        RepositoryEventConfiguration? config = null,
        ILogger<EventGenerationInterceptor<T>>? logger = null)
    {
        AddInterceptor(new EventGenerationInterceptor<T>(eventRepository, config, logger));
        return this;
    }

    #endregion
}

/// <summary>
/// Interface for repository interceptors.
/// Implement this to add cross-cutting concerns to repository operations.
/// </summary>
public interface IRepositoryInterceptor<T> where T : class, IDataModel
{
    // Create interceptors
    Task OnBeforeCreateAsync(T entity, ITransactionContext? tc = null);
    Task OnAfterCreateAsync(T entity, ITransactionContext? tc = null);
    Task OnCreateErrorAsync(T entity, Exception error, ITransactionContext? tc = null);
    
    Task OnBeforeCreateBatchAsync(IEnumerable<T> entities, ITransactionContext? tc = null);
    Task OnAfterCreateBatchAsync(IEnumerable<T> entities, ITransactionContext? tc = null);
    Task OnCreateBatchErrorAsync(IEnumerable<T> entities, Exception error, ITransactionContext? tc = null);
    
    // Update interceptors
    Task OnBeforeUpdateAsync(T entity, ITransactionContext? tc = null);
    Task OnAfterUpdateAsync(T entity, ITransactionContext? tc = null);
    Task OnUpdateErrorAsync(T entity, Exception error, ITransactionContext? tc = null);
    
    // Delete interceptors
    Task OnBeforeDeleteAsync(T entity, ITransactionContext? tc = null);
    Task OnAfterDeleteAsync(T entity, ITransactionContext? tc = null);
    Task OnDeleteErrorAsync(T entity, Exception error, ITransactionContext? tc = null);
}

/// <summary>
/// Base implementation of IRepositoryInterceptor with no-op methods.
/// Extend this class and override only the methods you need.
/// </summary>
public abstract class RepositoryInterceptorBase<T> : IRepositoryInterceptor<T> where T : class, IDataModel
{
    public virtual Task OnBeforeCreateAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnAfterCreateAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnCreateErrorAsync(T entity, Exception error, ITransactionContext? tc = null) => Task.CompletedTask;
    
    public virtual Task OnBeforeCreateBatchAsync(IEnumerable<T> entities, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnAfterCreateBatchAsync(IEnumerable<T> entities, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnCreateBatchErrorAsync(IEnumerable<T> entities, Exception error, ITransactionContext? tc = null) => Task.CompletedTask;
    
    public virtual Task OnBeforeUpdateAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnAfterUpdateAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnUpdateErrorAsync(T entity, Exception error, ITransactionContext? tc = null) => Task.CompletedTask;
    
    public virtual Task OnBeforeDeleteAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnAfterDeleteAsync(T entity, ITransactionContext? tc = null) => Task.CompletedTask;
    public virtual Task OnDeleteErrorAsync(T entity, Exception error, ITransactionContext? tc = null) => Task.CompletedTask;
}

/// <summary>
/// Logging interceptor that logs all repository operations.
/// </summary>
public class LoggingInterceptor<T> : RepositoryInterceptorBase<T> where T : class, IDataModel, IAppEntity
{
    private readonly ILogger<LoggingInterceptor<T>> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor<T>> logger)
    {
        _logger = logger;
    }

    public override Task OnBeforeCreateAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogDebug("Creating {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnAfterCreateAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogInformation("Created {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnCreateErrorAsync(T entity, Exception error, ITransactionContext? tc = null)
    {
        _logger.LogError(error, "Failed to create {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnBeforeUpdateAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogDebug("Updating {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnAfterUpdateAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogInformation("Updated {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnUpdateErrorAsync(T entity, Exception error, ITransactionContext? tc = null)
    {
        _logger.LogError(error, "Failed to update {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnBeforeDeleteAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogDebug("Deleting {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnAfterDeleteAsync(T entity, ITransactionContext? tc = null)
    {
        _logger.LogInformation("Deleted {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }

    public override Task OnDeleteErrorAsync(T entity, Exception error, ITransactionContext? tc = null)
    {
        _logger.LogError(error, "Failed to delete {EntityType} with ID {EntityId}", entity.EntityType, entity.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Validation interceptor that validates entities before operations.
/// </summary>
public class ValidationInterceptor<T> : RepositoryInterceptorBase<T> where T : class, IDataModel
{
    private readonly IValidator<T>? _validator;

    public ValidationInterceptor(IValidator<T>? validator = null)
    {
        _validator = validator;
    }

    public override async Task OnBeforeCreateAsync(T entity, ITransactionContext? tc = null)
    {
        if (_validator != null)
        {
            await _validator.ValidateAsync(entity);
        }
    }

    public override async Task OnBeforeUpdateAsync(T entity, ITransactionContext? tc = null)
    {
        if (_validator != null)
        {
            await _validator.ValidateAsync(entity);
        }
    }

    public override async Task OnBeforeCreateBatchAsync(IEnumerable<T> entities, ITransactionContext? tc = null)
    {
        if (_validator != null)
        {
            foreach (var entity in entities)
            {
                await _validator.ValidateAsync(entity);
            }
        }
    }
}

/// <summary>
/// Simple validator interface for entities.
/// </summary>
public interface IValidator<T>
{
    Task ValidateAsync(T entity);
}

/// <summary>
/// Event generation interceptor that generates events for repository operations.
/// Can be used to add event generation to repositories that don't inherit from EventAwareRepository.
/// </summary>
public class EventGenerationInterceptor<T> : RepositoryInterceptorBase<T> 
    where T : class, IDataModel, IAppEntity
{
    private readonly IRepository<AppEvent> _eventRepository;
    private readonly RepositoryEventConfiguration _config;
    private readonly ILogger<EventGenerationInterceptor<T>>? _logger;

    public EventGenerationInterceptor(
        IRepository<AppEvent> eventRepository,
        RepositoryEventConfiguration? config = null,
        ILogger<EventGenerationInterceptor<T>>? logger = null)
    {
        _eventRepository = eventRepository;
        _config = config ?? new RepositoryEventConfiguration
        {
            CreateEventTopic = $"{typeof(T).Name}.Created",
            UpdateEventTopic = $"{typeof(T).Name}.Updated",
            DeleteEventTopic = $"{typeof(T).Name}.Deleted"
        };
        _logger = logger;
    }

    public override async Task OnAfterCreateAsync(T entity, ITransactionContext? tc = null)
    {
        if (_config.GenerateCreateEvents && _config.ShouldGenerateEvent(entity, "create"))
        {
            var appEvent = new AppEvent
            {
                Topic = _config.GetEventTopic("create"),
                ActionId = _config.CreateEventAction,
                OriginEntityType = entity.EntityType,
                OriginEntityId = entity.Id,
                EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        _config.CustomEventDataProvider?.Invoke(entity, "create") ?? entity))
            };

            _logger?.LogDebug("Generating create event for {EntityType} {EntityId}", 
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
    }

    public override async Task OnAfterUpdateAsync(T entity, ITransactionContext? tc = null)
    {
        if (_config.GenerateUpdateEvents && _config.ShouldGenerateEvent(entity, "update"))
        {
            var appEvent = new AppEvent
            {
                Topic = _config.GetEventTopic("update"),
                ActionId = _config.UpdateEventAction,
                OriginEntityType = entity.EntityType,
                OriginEntityId = entity.Id,
                EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        _config.CustomEventDataProvider?.Invoke(entity, "update") ?? entity))
            };

            _logger?.LogDebug("Generating update event for {EntityType} {EntityId}", 
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
    }

    public override async Task OnAfterDeleteAsync(T entity, ITransactionContext? tc = null)
    {
        if (_config.GenerateDeleteEvents && _config.ShouldGenerateEvent(entity, "delete"))
        {
            var appEvent = new AppEvent
            {
                Topic = _config.GetEventTopic("delete"),
                ActionId = _config.DeleteEventAction,
                OriginEntityType = entity.EntityType,
                OriginEntityId = entity.Id,
                EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        _config.IncludeEntityInDeleteEvent 
                            ? entity 
                            : _config.CustomEventDataProvider?.Invoke(entity, "delete") ?? entity))
            };

            _logger?.LogDebug("Generating delete event for {EntityType} {EntityId}", 
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
    }
}