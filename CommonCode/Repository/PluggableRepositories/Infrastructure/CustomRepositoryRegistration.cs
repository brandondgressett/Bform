using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Repository.PluggableRepositories.Base;
using BFormDomain.DataModels;
using BFormDomain.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Repository.PluggableRepositories.Infrastructure;

/// <summary>
/// Extension methods for registering custom repositories with dependency injection.
/// Provides convenient methods to register event-aware and external data source repositories.
/// </summary>
public static class CustomRepositoryRegistration
{
    /// <summary>
    /// Registers an event-aware MongoDB repository that automatically generates events.
    /// </summary>
    /// <typeparam name="TInterface">The repository interface (e.g., IProductRepository)</typeparam>
    /// <typeparam name="TImplementation">The repository implementation</typeparam>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public static IServiceCollection AddEventAwareRepository<TInterface, TImplementation, TEntity>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class, IRepository<TEntity>
        where TImplementation : EventAwareRepository<TEntity>, TInterface
        where TEntity : class, IDataModel, IAppEntity
    {
        services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRepository<TEntity>), 
            provider => provider.GetRequiredService<TInterface>(), lifetime));
        
        return services;
    }

    /// <summary>
    /// Registers an external data source repository that polls external data and generates events.
    /// Also registers it as a hosted service for automatic polling.
    /// </summary>
    /// <typeparam name="TInterface">The repository interface</typeparam>
    /// <typeparam name="TImplementation">The repository implementation</typeparam>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public static IServiceCollection AddExternalDataSourceRepository<TInterface, TImplementation, TEntity>(
        this IServiceCollection services,
        TimeSpan? pollingInterval = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TInterface : class, IRepository<TEntity>
        where TImplementation : ExternalDataSourceRepository<TEntity>, TInterface
        where TEntity : class, IDataModel, IAppEntity, new()
    {
        // Register the repository
        services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRepository<TEntity>), 
            provider => provider.GetRequiredService<TInterface>(), lifetime));
        
        // Register as hosted service for automatic polling
        services.AddSingleton<IHostedService>(provider =>
        {
            var repository = provider.GetRequiredService<TInterface>();
            return (IHostedService)repository;
        });
        
        // Configure polling interval if provided
        if (pollingInterval.HasValue)
        {
            services.Configure<ExternalDataSourceOptions>(options =>
            {
                options.DefaultPollingInterval = pollingInterval.Value;
            });
        }
        
        return services;
    }

    /// <summary>
    /// Registers a custom repository with specific configuration.
    /// </summary>
    public static IServiceCollection AddCustomRepository<TInterface, TImplementation, TEntity>(
        this IServiceCollection services,
        Action<CustomRepositoryOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class, IRepository<TEntity>
        where TImplementation : class, TInterface
        where TEntity : class, IDataModel
    {
        var options = new CustomRepositoryOptions();
        configure(options);
        
        // Register the repository
        services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
        
        // Also register as IRepository<T> if requested
        if (options.RegisterAsGenericRepository)
        {
            services.Add(new ServiceDescriptor(typeof(IRepository<TEntity>), 
                provider => provider.GetRequiredService<TInterface>(), lifetime));
        }
        
        // Register event consumer if specified
        if (options.EventConsumerType != null)
        {
            services.AddScoped(options.EventConsumerType);
            services.AddHostedService<EventConsumerHostedService>();
        }
        
        // Register health check if specified
        if (options.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddTypeActivatedCheck<RepositoryHealthCheck<TEntity>>(
                    $"{typeof(TEntity).Name}Repository",
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                    options.HealthCheckTags,
                    Array.Empty<object>());
        }
        
        return services;
    }

    /// <summary>
    /// Registers multiple repositories from an assembly.
    /// </summary>
    public static IServiceCollection AddRepositoriesFromAssembly(
        this IServiceCollection services,
        Type assemblyMarkerType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var assembly = assemblyMarkerType.Assembly;
        
        // Find all repository implementations
        var repositoryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == typeof(IRepository<>)))
            .ToList();
        
        foreach (var repositoryType in repositoryTypes)
        {
            // Find the IRepository<T> interface
            var repositoryInterface = repositoryType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IRepository<>));
            
            if (repositoryInterface != null)
            {
                var entityType = repositoryInterface.GetGenericArguments()[0];
                
                // Check if it's an event-aware repository
                if (repositoryType.IsSubclassOf(typeof(EventAwareRepository<>).MakeGenericType(entityType)))
                {
                    // Find custom interface (e.g., IProductRepository)
                    var customInterface = repositoryType.GetInterfaces()
                        .FirstOrDefault(i => i != repositoryInterface && 
                            i.GetInterfaces().Contains(repositoryInterface));
                    
                    if (customInterface != null)
                    {
                        services.Add(new ServiceDescriptor(customInterface, repositoryType, lifetime));
                    }
                    
                    services.Add(new ServiceDescriptor(repositoryInterface, 
                        provider => provider.GetService(customInterface ?? repositoryType)!, 
                        lifetime));
                }
                // Check if it's an external data source repository
                else if (repositoryType.IsSubclassOf(typeof(ExternalDataSourceRepository<>).MakeGenericType(entityType)))
                {
                    var customInterface = repositoryType.GetInterfaces()
                        .FirstOrDefault(i => i != repositoryInterface && 
                            i.GetInterfaces().Contains(repositoryInterface));
                    
                    if (customInterface != null)
                    {
                        services.Add(new ServiceDescriptor(customInterface, repositoryType, ServiceLifetime.Singleton));
                    }
                    
                    services.Add(new ServiceDescriptor(repositoryInterface, 
                        provider => provider.GetService(customInterface ?? repositoryType)!, 
                        ServiceLifetime.Singleton));
                    
                    // Register as hosted service
                    services.AddSingleton<IHostedService>(provider =>
                    {
                        var repository = provider.GetService(customInterface ?? repositoryType);
                        return (IHostedService)repository!;
                    });
                }
                else
                {
                    // Regular repository
                    services.Add(new ServiceDescriptor(repositoryInterface, repositoryType, lifetime));
                }
            }
        }
        
        return services;
    }

    /// <summary>
    /// Configures event generation for all repositories.
    /// </summary>
    public static IServiceCollection ConfigureRepositoryEvents(
        this IServiceCollection services,
        Action<GlobalEventConfiguration> configure)
    {
        var config = new GlobalEventConfiguration();
        configure(config);
        
        services.AddSingleton(config);
        
        // Apply configuration to all event-aware repositories
        services.Configure<RepositoryEventConfiguration>(options =>
        {
            options.GenerateCreateEvents = config.GenerateCreateEvents;
            options.GenerateUpdateEvents = config.GenerateUpdateEvents;
            options.GenerateDeleteEvents = config.GenerateDeleteEvents;
            options.GenerateBatchEvents = config.GenerateBatchEvents;
            options.GenerateSystemOperationEvents = config.GenerateSystemOperationEvents;
        });
        
        return services;
    }

    /// <summary>
    /// Adds an event consumer for repository events.
    /// </summary>
    public static IServiceCollection AddRepositoryEventConsumer<TConsumer, TEntity>(
        this IServiceCollection services)
        where TConsumer : class, IAppEventConsumer
        where TEntity : class, IDataModel
    {
        services.AddScoped<TConsumer>();
        
        // Register topic subscriptions
        services.Configure<EventConsumerOptions>(options =>
        {
            var entityName = typeof(TEntity).Name;
            options.TopicSubscriptions.Add($"{entityName}.Created", typeof(TConsumer));
            options.TopicSubscriptions.Add($"{entityName}.Updated", typeof(TConsumer));
            options.TopicSubscriptions.Add($"{entityName}.Deleted", typeof(TConsumer));
        });
        
        return services;
    }

    /// <summary>
    /// Adds change detection services for external data sources.
    /// </summary>
    public static IServiceCollection AddChangeDetection<TEntity>(
        this IServiceCollection services,
        ChangeDetectionStrategy defaultStrategy = ChangeDetectionStrategy.Hash)
        where TEntity : class, IDataModel
    {
        services.AddSingleton<ExternalDataChangeDetector<TEntity>>();
        
        services.Configure<ChangeDetectionOptions>(options =>
        {
            options.DefaultStrategy = defaultStrategy;
            options.EntityTypeStrategies[typeof(TEntity).FullName!] = defaultStrategy;
        });
        
        return services;
    }
}

/// <summary>
/// Options for custom repository registration.
/// </summary>
public class CustomRepositoryOptions
{
    /// <summary>
    /// Whether to register the repository as IRepository<T> in addition to its interface.
    /// </summary>
    public bool RegisterAsGenericRepository { get; set; } = true;
    
    /// <summary>
    /// Type of event consumer to register for this repository's events.
    /// </summary>
    public Type? EventConsumerType { get; set; }
    
    /// <summary>
    /// Whether to add a health check for this repository.
    /// </summary>
    public bool AddHealthCheck { get; set; } = false;
    
    /// <summary>
    /// Tags to add to the health check.
    /// </summary>
    public List<string>? HealthCheckTags { get; set; }
}

/// <summary>
/// Global configuration for repository event generation.
/// </summary>
public class GlobalEventConfiguration
{
    public bool GenerateCreateEvents { get; set; } = true;
    public bool GenerateUpdateEvents { get; set; } = true;
    public bool GenerateDeleteEvents { get; set; } = true;
    public bool GenerateBatchEvents { get; set; } = true;
    public bool GenerateSystemOperationEvents { get; set; } = true;
}

/// <summary>
/// Options for external data sources.
/// </summary>
public class ExternalDataSourceOptions
{
    public TimeSpan DefaultPollingInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Options for event consumers.
/// </summary>
public class EventConsumerOptions
{
    public Dictionary<string, Type> TopicSubscriptions { get; set; } = new();
}

/// <summary>
/// Options for change detection.
/// </summary>
public class ChangeDetectionOptions
{
    public ChangeDetectionStrategy DefaultStrategy { get; set; } = ChangeDetectionStrategy.Hash;
    public Dictionary<string, ChangeDetectionStrategy> EntityTypeStrategies { get; set; } = new();
}

/// <summary>
/// Health check for custom repositories.
/// </summary>
public class RepositoryHealthCheck<TEntity> : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    where TEntity : class, IDataModel
{
    private readonly IRepository<TEntity> _repository;
    private readonly ILogger<RepositoryHealthCheck<TEntity>> _logger;

    public RepositoryHealthCheck(
        IRepository<TEntity> repository,
        ILogger<RepositoryHealthCheck<TEntity>> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to count entities
            var count = await _repository.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["entityType"] = typeof(TEntity).Name,
                ["entityCount"] = count
            };
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"{typeof(TEntity).Name} repository is healthy", 
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {EntityType} repository", typeof(TEntity).Name);
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                $"{typeof(TEntity).Name} repository is unhealthy",
                ex);
        }
    }
}

/// <summary>
/// Hosted service for running event consumers.
/// </summary>
public class EventConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventConsumerOptions _options;
    private readonly ILogger<EventConsumerHostedService> _logger;

    public EventConsumerHostedService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<EventConsumerOptions> options,
        ILogger<EventConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event consumer hosted service started");
        
        // This is a placeholder - actual implementation would subscribe to events
        // and route them to appropriate consumers based on topic subscriptions
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}