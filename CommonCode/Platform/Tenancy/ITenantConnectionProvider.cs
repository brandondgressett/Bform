using BFormDomain.Mongo;
using BFormDomain.Repository;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Provides tenant-specific database and storage connections.
/// Implementations handle connection string resolution, caching, and security.
/// </summary>
public interface ITenantConnectionProvider
{
    /// <summary>
    /// Get database connection options for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MongoDB repository options configured for the tenant</returns>
    Task<MongoRepositoryOptions> GetDatabaseConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get storage connection options for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage connection options configured for the tenant</returns>
    Task<StorageConnectionOptions> GetStorageConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refresh cached connections for a tenant (useful after connection updates)
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RefreshConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test connectivity for a tenant's connections
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of connection test results by type</returns>
    Task<Dictionary<ConnectionType, bool>> TestConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default);
}