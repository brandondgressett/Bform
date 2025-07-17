using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Factory for creating and managing tenant-specific content repositories.
/// Ensures each tenant has its own isolated content repository instance.
/// </summary>
public class TenantContentRepositoryFactory : ITenantContentRepositoryFactory
{
    private readonly ConcurrentDictionary<Guid, ITenantAwareApplicationPlatformContent> _tenantRepositories = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantContentRepositoryFactory> _logger;
    private readonly IApplicationAlert _alerts;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly FileApplicationPlatformContentOptions _contentOptions;
    private readonly object _lockObject = new();

    public TenantContentRepositoryFactory(
        IServiceProvider serviceProvider,
        ILogger<TenantContentRepositoryFactory> logger,
        IApplicationAlert alerts,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        IOptions<FileApplicationPlatformContentOptions> contentOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
        _contentOptions = contentOptions?.Value ?? throw new ArgumentNullException(nameof(contentOptions));
    }

    /// <summary>
    /// Gets or creates a content repository for the specified tenant.
    /// </summary>
    public ITenantAwareApplicationPlatformContent GetTenantContentRepository(Guid tenantId)
    {
        if (!_multiTenancyOptions.Enabled)
        {
            // In single-tenant mode, always return the same instance
            tenantId = _multiTenancyOptions.GlobalTenantId;
        }

        return _tenantRepositories.GetOrAdd(tenantId, id =>
        {
            _logger.LogInformation("Creating content repository for tenant {TenantId}", id);
            return CreateTenantRepository(id);
        });
    }

    /// <summary>
    /// Gets or creates a content repository for the current tenant context.
    /// </summary>
    public ITenantAwareApplicationPlatformContent GetCurrentTenantContentRepository()
    {
        var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
        var tenantId = tenantContext.CurrentTenantId ?? _multiTenancyOptions.GlobalTenantId;
        return GetTenantContentRepository(tenantId);
    }

    /// <summary>
    /// Removes a tenant's content repository from the cache.
    /// Useful when tenant content needs to be reloaded.
    /// </summary>
    public void RemoveTenantRepository(Guid tenantId)
    {
        if (_tenantRepositories.TryRemove(tenantId, out var repository))
        {
            _logger.LogInformation("Removed content repository for tenant {TenantId}", tenantId);
            
            // If the repository implements IDisposable, dispose it
            if (repository is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Clears all cached tenant repositories.
    /// </summary>
    public void ClearAllRepositories()
    {
        var tenantIds = _tenantRepositories.Keys.ToList();
        foreach (var tenantId in tenantIds)
        {
            RemoveTenantRepository(tenantId);
        }
        
        _logger.LogInformation("Cleared all tenant content repositories");
    }
    
    /// <summary>
    /// Clears the cached content repository for a specific tenant.
    /// </summary>
    public void ClearTenantCache(Guid tenantId)
    {
        RemoveTenantRepository(tenantId);
    }

    /// <summary>
    /// Gets the number of currently cached tenant repositories.
    /// </summary>
    public int CachedRepositoryCount => _tenantRepositories.Count;

    /// <summary>
    /// Gets all currently cached tenant IDs.
    /// </summary>
    public IEnumerable<Guid> CachedTenantIds => _tenantRepositories.Keys;

    private ITenantAwareApplicationPlatformContent CreateTenantRepository(Guid tenantId)
    {
        try
        {
            // Create tenant-specific options
            var tenantContentOptions = CreateTenantContentOptions(tenantId);

            // Create a new instance of the content repository for this tenant
            // Using a factory delegate to create instances with tenant-specific configuration
            var repository = new TenantAwareFileApplicationPlatformContent(
                tenantId,
                _serviceProvider.GetRequiredService<ILogger<TenantAwareFileApplicationPlatformContent>>(),
                _alerts,
                _serviceProvider.GetServices<IContentDomainSource>(),
                _serviceProvider.GetServices<IEntityInstanceLogic>(),
                Options.Create(tenantContentOptions));

            _logger.LogInformation(
                "Created content repository for tenant {TenantId} with base folder: {BaseFolder}",
                tenantId, tenantContentOptions.BaseFolder);

            return repository;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create content repository for tenant {TenantId}", tenantId);
            _alerts.RaiseAlert(
                ApplicationAlertKind.System,
                LogLevel.Critical,
                $"Failed to create content repository for tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    private FileApplicationPlatformContentOptions CreateTenantContentOptions(Guid tenantId)
    {
        // Create tenant-specific folder structure
        var baseFolder = _contentOptions.BaseFolder;
        var schemaFolder = _contentOptions.SchemaFolder;

        if (_multiTenancyOptions.Enabled && tenantId != _multiTenancyOptions.GlobalTenantId)
        {
            // For multi-tenant mode, create tenant-specific folders
            baseFolder = Path.Combine(baseFolder, "tenants", tenantId.ToString());
            // Schemas are shared across tenants, so keep the same schema folder
        }

        return new FileApplicationPlatformContentOptions
        {
            BaseFolder = baseFolder,
            SchemaFolder = schemaFolder
        };
    }
}

/// <summary>
/// Interface for the tenant content repository factory.
/// </summary>
public interface ITenantContentRepositoryFactory
{
    /// <summary>
    /// Gets or creates a content repository for the specified tenant.
    /// </summary>
    ITenantAwareApplicationPlatformContent GetTenantContentRepository(Guid tenantId);

    /// <summary>
    /// Gets or creates a content repository for the current tenant context.
    /// </summary>
    ITenantAwareApplicationPlatformContent GetCurrentTenantContentRepository();

    /// <summary>
    /// Removes a tenant's content repository from the cache.
    /// </summary>
    void RemoveTenantRepository(Guid tenantId);

    /// <summary>
    /// Clears all cached tenant repositories.
    /// </summary>
    void ClearAllRepositories();
    
    /// <summary>
    /// Clears the cached content repository for a specific tenant.
    /// </summary>
    void ClearTenantCache(Guid tenantId);

    /// <summary>
    /// Gets the number of currently cached tenant repositories.
    /// </summary>
    int CachedRepositoryCount { get; }

    /// <summary>
    /// Gets all currently cached tenant IDs.
    /// </summary>
    IEnumerable<Guid> CachedTenantIds { get; }
}