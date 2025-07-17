using BFormDomain.CommonCode.Platform.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;

/// <summary>
/// Tenant-aware wrapper for IManagedFilePersistence that ensures files are isolated by tenant.
/// This wrapper automatically prefixes container names with tenant identifiers to ensure
/// complete file isolation between tenants.
/// </summary>
public class TenantAwareManagedFilePersistence : IManagedFilePersistence
{
    private readonly IManagedFilePersistence _innerPersistence;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantAwareManagedFilePersistence> _logger;
    private readonly MultiTenancyOptions _multiTenancyOptions;

    public TenantAwareManagedFilePersistence(
        IManagedFilePersistence innerPersistence,
        ITenantContext tenantContext,
        ILogger<TenantAwareManagedFilePersistence> logger,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
    {
        _innerPersistence = innerPersistence ?? throw new ArgumentNullException(nameof(innerPersistence));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
    }

    /// <summary>
    /// Upserts a file with tenant isolation applied to the container name.
    /// </summary>
    public async Task UpsertAsync(string container, string id, Stream stream, CancellationToken ct)
    {
        var tenantContainer = GetTenantContainer(container);
        _logger.LogDebug("Upserting file {FileId} to tenant container {TenantContainer}", id, tenantContainer);
        await _innerPersistence.UpsertAsync(tenantContainer, id, stream, ct);
    }

    /// <summary>
    /// Retrieves a file with tenant isolation applied to the container name.
    /// </summary>
    public async Task<Stream> RetrieveAsync(string container, string id, CancellationToken ct)
    {
        var tenantContainer = GetTenantContainer(container);
        _logger.LogDebug("Retrieving file {FileId} from tenant container {TenantContainer}", id, tenantContainer);
        return await _innerPersistence.RetrieveAsync(tenantContainer, id, ct);
    }

    /// <summary>
    /// Deletes a file with tenant isolation applied to the container name.
    /// </summary>
    public async Task DeleteAsync(string container, string id, CancellationToken ct)
    {
        var tenantContainer = GetTenantContainer(container);
        _logger.LogDebug("Deleting file {FileId} from tenant container {TenantContainer}", id, tenantContainer);
        await _innerPersistence.DeleteAsync(tenantContainer, id, ct);
    }

    /// <summary>
    /// Checks if a file exists with tenant isolation applied to the container name.
    /// </summary>
    public async Task<bool> Exists(string container, string id, CancellationToken ct)
    {
        var tenantContainer = GetTenantContainer(container);
        return await _innerPersistence.Exists(tenantContainer, id, ct);
    }

    /// <summary>
    /// Gets the tenant-specific container name by prefixing with tenant ID.
    /// In single-tenant mode, returns the original container name.
    /// </summary>
    private string GetTenantContainer(string container)
    {
        // In single-tenant mode, don't prefix containers
        if (!_multiTenancyOptions.Enabled)
        {
            return container;
        }

        // Get current tenant ID
        var tenantId = _tenantContext.CurrentTenantId ?? _multiTenancyOptions.GlobalTenantId;
        
        // Create tenant-specific container name using underscore as delimiter
        // We use underscore instead of slash to comply with PhysicalFilePersistence validation
        var tenantContainer = $"tenant_{tenantId}_{container}";
        
        _logger.LogTrace("Mapped container {Container} to tenant container {TenantContainer}", 
            container, tenantContainer);
        
        return tenantContainer;
    }
}