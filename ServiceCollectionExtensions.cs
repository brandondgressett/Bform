using BFormDomain.Repository;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Repository;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Repository.Mongo;
using BFormDomain.CommonCode.Utility;
using BFormDomain.MessageBus;
using BFormDomain.MessageBus.InMemory;
using BFormDomain.MessageBus.RabbitMQ;
using BFormDomain.MessageBus.AzureServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using BFormDomain.Mongo;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.CommonCode.Platform.Content;
using Microsoft.Extensions.Caching.Memory;
namespace BFormDomain;

/// <summary>
/// Extension methods for registering BFormDomain services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all BFormDomain services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="useQuartzScheduler">Whether to use Quartz.NET scheduler instead of the legacy scheduler.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBFormDomain(
        this IServiceCollection services, 
        IConfiguration configuration,
        bool useQuartzScheduler = true)
    {
        // Register core repository services
        services.AddSingleton<IRepositoryFactory, MongoRepositoryFactory>();
        
        // Register Message Bus
        services.AddBFormMessageBus(configuration);
        
        // Register AppEvent system
        services.AddSingleton<AppEventSink>();
        services.AddSingleton<AppEventDistributer>();
        
        // Register File Storage services
        services.AddBFormFileStorage(configuration);
        
        // Register Rule Engine components
        services.AddStandardRuleElements();
        
        // Register scheduler based on configuration
        if (useQuartzScheduler)
        {
            // Get MongoDB connection string from configuration
            var mongoConnectionString = configuration.GetConnectionString("MongoDB") 
                ?? configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDB connection string not found in configuration");
            
            // Configure Quartz from configuration
            services.AddBFormQuartzScheduler(mongoConnectionString, options =>
            {
                // Load settings from configuration if available
                var quartzSection = configuration.GetSection("Quartz");
                if (quartzSection.Exists())
                {
                    options.SchedulerName = quartzSection["SchedulerName"] ?? options.SchedulerName;
                    options.DatabaseName = quartzSection["DatabaseName"] ?? options.DatabaseName;
                    options.CollectionPrefix = quartzSection["CollectionPrefix"] ?? options.CollectionPrefix;
                    
                    if (int.TryParse(quartzSection["ThreadCount"], out var threadCount))
                        options.ThreadCount = threadCount;
                    
                    if (bool.TryParse(quartzSection["EnableClustering"], out var enableClustering))
                        options.EnableClustering = enableClustering;
                    
                    if (int.TryParse(quartzSection["ClusterCheckinInterval"], out var clusterCheckinInterval))
                        options.ClusterCheckinInterval = clusterCheckinInterval;
                    
                    if (int.TryParse(quartzSection["MisfireThreshold"], out var misfireThreshold))
                        options.MisfireThreshold = misfireThreshold;
                }
            });
        }
        else
        {
            // Register legacy scheduler (to be removed in future)
            services.AddSingleton<BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic, QuartzSchedulerLogic>();
            // services.AddHostedService<SchedulerBackgroundWorker>(); // Obsolete - use Quartz scheduler instead
        }
        
        return services;
    }
    
    /// <summary>
    /// Adds only the Quartz scheduler to the service collection.
    /// Use this if you want to configure other BFormDomain services separately.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBFormQuartzScheduler(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoConnectionString = configuration.GetConnectionString("MongoDB") 
            ?? configuration["MongoDB:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDB connection string not found in configuration");
        
        return services.AddBFormQuartzScheduler(mongoConnectionString, options =>
        {
            var quartzSection = configuration.GetSection("Quartz");
            if (quartzSection.Exists())
            {
                options.SchedulerName = quartzSection["SchedulerName"] ?? options.SchedulerName;
                options.DatabaseName = quartzSection["DatabaseName"] ?? options.DatabaseName;
                options.CollectionPrefix = quartzSection["CollectionPrefix"] ?? options.CollectionPrefix;
                
                if (int.TryParse(quartzSection["ThreadCount"], out var threadCount))
                    options.ThreadCount = threadCount;
                
                if (bool.TryParse(quartzSection["EnableClustering"], out var enableClustering))
                    options.EnableClustering = enableClustering;
                
                if (int.TryParse(quartzSection["ClusterCheckinInterval"], out var clusterCheckinInterval))
                    options.ClusterCheckinInterval = clusterCheckinInterval;
                
                if (int.TryParse(quartzSection["MisfireThreshold"], out var misfireThreshold))
                    options.MisfireThreshold = misfireThreshold;
            }
        }, healthOptions =>
        {
            // Configure health check options from configuration if available
            var healthSection = configuration.GetSection("Quartz:HealthCheck");
            if (healthSection.Exists())
            {
                if (double.TryParse(healthSection["MaxThreadPoolUsage"], out var maxThreadPoolUsage))
                    healthOptions.MaxThreadPoolUsage = maxThreadPoolUsage;
                
                if (bool.TryParse(healthSection["CheckMisfiredJobs"], out var checkMisfiredJobs))
                    healthOptions.CheckMisfiredJobs = checkMisfiredJobs;
                
                if (int.TryParse(healthSection["MaxMisfiredJobs"], out var maxMisfiredJobs))
                    healthOptions.MaxMisfiredJobs = maxMisfiredJobs;
                
                if (int.TryParse(healthSection["MisfireThresholdSeconds"], out var misfireThresholdSeconds))
                    healthOptions.MisfireThresholdSeconds = misfireThresholdSeconds;
            }
        });
    }
    
    /// <summary>
    /// Adds the message bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBFormMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var messageBusType = configuration["MessageBus:Type"]?.ToLowerInvariant() ?? "inmemory";
        
        switch (messageBusType)
        {
            case "rabbitmq":
                services.Configure<RabbitMQOptions>(configuration.GetSection("MessageBus:RabbitMQ"));
                
                // Register RabbitMQ implementations
                services.AddSingleton<IMessageBusSpecifier, RabbitMQMessageBus>();
                
                // Register keyed services for different topologies
                services.AddKeyedSingleton<IMessageBusSpecifier, RabbitMQMessageBus>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedSingleton<IMessageBusSpecifier, RabbitMQMessageBus>(MessageBusTopology.LocalProcessOnly.ToString());
                
                // Register message components with factory pattern
                services.AddTransient<IMessagePublisher, RabbitMQMessagePublisher>();
                services.AddTransient<IMessageListener, RabbitMQMessageListener>();
                services.AddTransient<IMessageRetriever, RabbitMQMessageRetriever>();
                
                // Register keyed message components
                services.AddKeyedTransient<IMessagePublisher, RabbitMQMessagePublisher>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageListener, RabbitMQMessageListener>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageRetriever, RabbitMQMessageRetriever>(MessageBusTopology.Distributed.ToString());
                
                services.AddKeyedTransient<IMessagePublisher, RabbitMQMessagePublisher>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageListener, RabbitMQMessageListener>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageRetriever, RabbitMQMessageRetriever>(MessageBusTopology.LocalProcessOnly.ToString());
                
                break;
                
            case "azureservicebus":
            case "servicebus":
                services.Configure<AzureServiceBusOptions>(configuration.GetSection("MessageBus:AzureServiceBus"));
                
                // Register Azure Service Bus implementations
                services.AddSingleton<IMessageBusSpecifier, AzureServiceBusMessageBus>();
                
                // Register keyed services for different topologies
                services.AddKeyedSingleton<IMessageBusSpecifier, AzureServiceBusMessageBus>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedSingleton<IMessageBusSpecifier, AzureServiceBusMessageBus>(MessageBusTopology.LocalProcessOnly.ToString());
                
                // Register message components with factory pattern
                services.AddTransient<IMessagePublisher, AzureServiceBusMessagePublisher>();
                services.AddTransient<IMessageListener, AzureServiceBusMessageListener>();
                services.AddTransient<IMessageRetriever, AzureServiceBusMessageRetriever>();
                
                // Register keyed message components
                services.AddKeyedTransient<IMessagePublisher, AzureServiceBusMessagePublisher>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageListener, AzureServiceBusMessageListener>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageRetriever, AzureServiceBusMessageRetriever>(MessageBusTopology.Distributed.ToString());
                
                services.AddKeyedTransient<IMessagePublisher, AzureServiceBusMessagePublisher>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageListener, AzureServiceBusMessageListener>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageRetriever, AzureServiceBusMessageRetriever>(MessageBusTopology.LocalProcessOnly.ToString());
                
                break;
                
            case "inmemory":
            default:
                // Register in-memory implementations
                services.AddSingleton<IMessageBusSpecifier, MemMessageBus>();
                
                // Register keyed services for different topologies
                services.AddKeyedSingleton<IMessageBusSpecifier, MemMessageBus>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedSingleton<IMessageBusSpecifier, MemMessageBus>(MessageBusTopology.LocalProcessOnly.ToString());
                
                // Register message components
                services.AddTransient<IMessagePublisher, MemMessagePublisher>();
                services.AddTransient<IMessageListener, MemMessageListener>();
                services.AddTransient<IMessageRetriever, MemMessageRetriever>();
                
                // Register keyed message components
                services.AddKeyedTransient<IMessagePublisher, MemMessagePublisher>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageListener, MemMessageListener>(MessageBusTopology.Distributed.ToString());
                services.AddKeyedTransient<IMessageRetriever, MemMessageRetriever>(MessageBusTopology.Distributed.ToString());
                
                services.AddKeyedTransient<IMessagePublisher, MemMessagePublisher>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageListener, MemMessageListener>(MessageBusTopology.LocalProcessOnly.ToString());
                services.AddKeyedTransient<IMessageRetriever, MemMessageRetriever>(MessageBusTopology.LocalProcessOnly.ToString());
                
                break;
        }
        
        // Register KeyInject resolvers
        services.AddSingleton<KeyInject<string, IMessageBusSpecifier>.ServiceResolver>(serviceProvider => key =>
        {
            return serviceProvider.GetRequiredKeyedService<IMessageBusSpecifier>(key);
        });
        
        services.AddSingleton<KeyInject<string, IMessagePublisher>.ServiceResolver>(serviceProvider => key =>
        {
            return serviceProvider.GetRequiredKeyedService<IMessagePublisher>(key);
        });
        
        services.AddSingleton<KeyInject<string, IMessageListener>.ServiceResolver>(serviceProvider => key =>
        {
            return serviceProvider.GetRequiredKeyedService<IMessageListener>(key);
        });
        
        services.AddSingleton<KeyInject<string, IMessageRetriever>.ServiceResolver>(serviceProvider => key =>
        {
            return serviceProvider.GetRequiredKeyedService<IMessageRetriever>(key);
        });
        
        return services;
    }

    /// <summary>
    /// Adds multi-tenancy support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure multi-tenancy options
        services.Configure<MultiTenancyOptions>(configuration.GetSection(MultiTenancyOptions.SectionName));
        
        // Get multi-tenancy options to determine what to register
        var multiTenancySection = configuration.GetSection(MultiTenancyOptions.SectionName);
        var isEnabled = multiTenancySection.GetValue<bool>("Enabled");

        // Always register core tenant services (needed even in single-tenant mode for global tenant)
        services.AddScoped<TenantRepository>();
        services.AddScoped<TenantConnectionRepository>();
        services.AddScoped<TenantInitializationService>();
        services.AddScoped<TenantWorkflowService>();
        services.AddScoped<TenantMetricsService>();
        services.AddScoped<TenantConfigurationValidator>();
        
        // Register connection pool for multi-tenant scenarios
        services.AddSingleton<TenantConnectionPool>();
        
        // Register content repository factory
        services.AddSingleton<ITenantContentRepositoryFactory, TenantContentRepositoryFactory>();

        // Register tenant context services
        services.AddScoped<ITenantContext, TenantContext>();

        if (isEnabled)
        {
            // Multi-tenancy is enabled - register full multi-tenant stack
            RegisterMultiTenantServices(services, configuration);
        }
        else
        {
            // Single-tenant mode - register minimal services
            RegisterSingleTenantServices(services, configuration);
        }

        // Always register the global tenant initializer
        services.AddHostedService<GlobalTenantInitializer>();

        return services;
    }

    private static void RegisterMultiTenantServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register connection providers based on configuration
        var connectionProvider = configuration.GetValue<string>("MultiTenancy:ConnectionProvider") ?? "Cached";

        switch (connectionProvider.ToLowerInvariant())
        {
            case "local":
                services.AddScoped<ITenantConnectionProvider, LocalConnectionProvider>();
                break;
            case "azurekeyvault":
            case "keyvault":
                // Configure Azure Key Vault options
                services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SectionName));
                services.AddScoped<ITenantConnectionProvider, AzureKeyVaultConnectionProvider>();
                break;
            case "cached":
            default:
                // Register both local provider and cached wrapper
                services.AddScoped<LocalConnectionProvider>();
                services.AddScoped<ITenantConnectionProvider>(serviceProvider =>
                {
                    var localProvider = serviceProvider.GetRequiredService<LocalConnectionProvider>();
                    var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                    var options = serviceProvider.GetRequiredService<IOptions<MultiTenancyOptions>>();
                    var logger = serviceProvider.GetRequiredService<ILogger<CachedConnectionProvider>>();
                    return new CachedConnectionProvider(localProvider, memoryCache, options, logger);
                });
                break;
        }

        // Register storage connection options for tenant-specific storage
        services.Configure<StorageConnectionOptions>(configuration.GetSection("Storage"));
        
        // Register repository factory for multi-tenant mode
        services.AddScoped<ITenantAwareRepositoryFactory, TenantAwareRepositoryFactory>();
        
        // Register tenant boundary enforcement
        services.AddSingleton<TenantBoundaryEnforcer>();
    }

    private static void RegisterSingleTenantServices(IServiceCollection services, IConfiguration configuration)
    {
        // In single-tenant mode, use simplified connection provider
        services.AddScoped<ITenantConnectionProvider, LocalConnectionProvider>();
        
        // Still configure storage options for consistency
        services.Configure<StorageConnectionOptions>(configuration.GetSection("Storage"));
        
        // Register repository factory for single-tenant mode
        services.AddScoped<ITenantAwareRepositoryFactory, TenantAwareRepositoryFactory>();
        
        // Register tenant boundary enforcement (no-op in single-tenant mode)
        services.AddSingleton<TenantBoundaryEnforcer>();
    }
    
    /// <summary>
    /// Adds file storage services with multi-tenancy support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBFormFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure file storage options
        services.Configure<PhysicalFilePersistenceOptions>(
            configuration.GetSection("FileStorage:Physical"));
        
        // Check which storage provider to use
        var storageProvider = configuration["FileStorage:Provider"] ?? "Physical";
        
        switch (storageProvider.ToLowerInvariant())
        {
            case "physical":
            case "filesystem":
                // Register physical file persistence as the inner implementation
                services.AddScoped<PhysicalFilePersistence>();
                
                // Register the tenant-aware wrapper as IManagedFilePersistence
                services.AddScoped<IManagedFilePersistence>(serviceProvider =>
                {
                    var multiTenancyOptions = serviceProvider.GetRequiredService<IOptions<MultiTenancyOptions>>();
                    
                    // In single-tenant mode, use PhysicalFilePersistence directly
                    if (!multiTenancyOptions.Value.Enabled)
                    {
                        return serviceProvider.GetRequiredService<PhysicalFilePersistence>();
                    }
                    
                    // In multi-tenant mode, wrap with tenant-aware implementation
                    return new TenantAwareManagedFilePersistence(
                        serviceProvider.GetRequiredService<PhysicalFilePersistence>(),
                        serviceProvider.GetRequiredService<ITenantContext>(),
                        serviceProvider.GetRequiredService<ILogger<TenantAwareManagedFilePersistence>>(),
                        multiTenancyOptions);
                });
                break;
                
            case "azureblob":
            case "azure":
                // Register Azure blob storage (to be implemented)
                throw new NotImplementedException(
                    "Azure Blob storage provider is not yet implemented for multi-tenancy");
                
            default:
                throw new InvalidOperationException(
                    $"Unknown file storage provider: {storageProvider}. " +
                    "Supported providers are: Physical, AzureBlob");
        }
        
        // Register ManagedFileLogic
        services.AddScoped<ManagedFileLogic>();
        services.AddScoped<ManagedFileRepository>();
        
        return services;
    }
}