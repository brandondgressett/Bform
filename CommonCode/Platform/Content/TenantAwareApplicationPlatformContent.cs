using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Tenant-aware implementation of IApplicationPlatformContent that provides
/// isolated content for each tenant. Each tenant has its own set of rules,
/// forms, templates, and other content types.
/// </summary>
public class TenantAwareApplicationPlatformContent : IApplicationPlatformContent
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantAwareApplicationPlatformContent> _logger;
    private readonly FileApplicationPlatformContentOptions _baseOptions;
    private readonly ConcurrentDictionary<string, IApplicationPlatformContent> _tenantContentStores = new();
    private readonly IApplicationPlatformContent? _globalContent;

    public TenantAwareApplicationPlatformContent(
        ITenantContext tenantContext,
        ITenantRegistry tenantRegistry,
        IServiceProvider serviceProvider,
        IOptions<FileApplicationPlatformContentOptions> options,
        ILogger<TenantAwareApplicationPlatformContent> logger)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _tenantRegistry = tenantRegistry ?? throw new ArgumentNullException(nameof(tenantRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _baseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize global content for system-level operations
        _globalContent = CreateContentStore(null);
    }

    public IEnumerable<ContentDomain> Domains => GetCurrentTenantContent().Domains;

    public JSchema LoadEmbeddedSchema<T>() where T : IContentType
    {
        // Schemas are shared across tenants
        return _globalContent?.LoadEmbeddedSchema<T>() 
            ?? throw new InvalidOperationException("Global content store not initialized");
    }

    public IList<T> GetAllContent<T>() where T : IContentType
    {
        return GetCurrentTenantContent().GetAllContent<T>();
    }

    public T? GetContentByName<T>(string name) where T : IContentType
    {
        return GetCurrentTenantContent().GetContentByName<T>(name);
    }

    public ContentElement? ViewContentType(string name)
    {
        return GetCurrentTenantContent().ViewContentType(name);
    }

    public IList<T> GetMatchingAny<T>(params string[] tags) where T : IContentType
    {
        return GetCurrentTenantContent().GetMatchingAny<T>(tags);
    }

    public IList<T> GetMatchingAll<T>(params string[] tags) where T : IContentType
    {
        return GetCurrentTenantContent().GetMatchingAll<T>(tags);
    }

    public string? GetFreeJson(string name)
    {
        return GetCurrentTenantContent().GetFreeJson(name);
    }

    private IApplicationPlatformContent GetCurrentTenantContent()
    {
        var tenantId = _tenantContext.TenantId;
        
        // If no tenant context, use global content
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogDebug("No tenant context found, using global content store");
            return _globalContent ?? throw new InvalidOperationException("Global content store not initialized");
        }

        // Get or create tenant-specific content store
        return _tenantContentStores.GetOrAdd(tenantId, id =>
        {
            _logger.LogInformation("Initializing content store for tenant {TenantId}", id);
            return CreateContentStore(id);
        });
    }

    private IApplicationPlatformContent CreateContentStore(string? tenantId)
    {
        // Create tenant-specific options
        var tenantOptions = new FileApplicationPlatformContentOptions
        {
            BaseFolder = string.IsNullOrEmpty(tenantId) 
                ? Path.Combine(_baseOptions.BaseFolder, "global")
                : Path.Combine(_baseOptions.BaseFolder, "tenants", tenantId),
            SchemaFolder = _baseOptions.SchemaFolder // Schemas are shared
        };

        // Ensure tenant folder exists
        if (!Directory.Exists(tenantOptions.BaseFolder))
        {
            _logger.LogInformation("Creating content folder for tenant {TenantId} at {Path}", 
                tenantId ?? "global", tenantOptions.BaseFolder);
            Directory.CreateDirectory(tenantOptions.BaseFolder);
        }

        // Create options wrapper
        var optionsWrapper = Options.Create(tenantOptions);

        // Get required services from service provider
        var fileLogger = (ILogger<FileApplicationPlatformContent>)_serviceProvider.GetService(typeof(ILogger<FileApplicationPlatformContent>))!;
        var alert = (IApplicationAlert)_serviceProvider.GetService(typeof(IApplicationAlert))!;
        var contentDomainSources = (IEnumerable<IContentDomainSource>)_serviceProvider.GetService(typeof(IEnumerable<IContentDomainSource>))!;
        var instanceConsumers = (IEnumerable<IEntityInstanceLogic>)_serviceProvider.GetService(typeof(IEnumerable<IEntityInstanceLogic>))!;

        return new FileApplicationPlatformContent(
            fileLogger,
            alert,
            contentDomainSources,
            instanceConsumers,
            optionsWrapper);
    }

    /// <summary>
    /// Preload content for all active tenants. This can be called during
    /// application startup to warm up the content cache.
    /// </summary>
    public async Task PreloadAllTenantContentAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Preloading content for all active tenants");

        var tenants = await _tenantRegistry.GetAllTenantsAsync(cancellationToken);
        
        var loadTasks = tenants
            .Where(t => t.IsActive)
            .Select(tenant => Task.Run(() =>
            {
                try
                {
                    _tenantContentStores.GetOrAdd(tenant.Id.ToString(), id =>
                    {
                        _logger.LogInformation("Preloading content for tenant {TenantId}", id);
                        return CreateContentStore(id);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to preload content for tenant {TenantId}", tenant.Id);
                }
            }, cancellationToken));

        await Task.WhenAll(loadTasks);
        
        _logger.LogInformation("Completed preloading content for {Count} tenants", _tenantContentStores.Count);
    }

    /// <summary>
    /// Clear cached content for a specific tenant. Useful when tenant
    /// content has been updated.
    /// </summary>
    public void ClearTenantCache(string tenantId)
    {
        if (_tenantContentStores.TryRemove(tenantId, out var removed))
        {
            _logger.LogInformation("Cleared content cache for tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Clear all cached content. Forces reload on next access.
    /// </summary>
    public void ClearAllCaches()
    {
        _tenantContentStores.Clear();
        _logger.LogInformation("Cleared all tenant content caches");
    }
}