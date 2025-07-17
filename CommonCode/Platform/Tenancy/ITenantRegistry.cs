namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Provides access to tenant information in the system
/// </summary>
public interface ITenantRegistry
{
    /// <summary>
    /// Get all tenants in the system
    /// </summary>
    Task<IEnumerable<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific tenant by ID
    /// </summary>
    Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a tenant exists
    /// </summary>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);
}