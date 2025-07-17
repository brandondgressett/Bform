using BFormDomain.DataModels;
using BFormDomain.Repository;
using System;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Repository
{
    /// <summary>
    /// Factory interface for creating tenant-specific repository instances.
    /// This allows for proper isolation of database connections and operations per tenant.
    /// </summary>
    public interface ITenantAwareRepositoryFactory
    {
        /// <summary>
        /// Creates a repository instance for the specified tenant.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="tenantId">The tenant ID to create the repository for</param>
        /// <returns>A repository instance scoped to the specified tenant</returns>
        Task<IRepository<T>> CreateRepositoryAsync<T>(Guid tenantId) where T : class, IDataModel;

        /// <summary>
        /// Creates a repository instance for the current tenant from context.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <returns>A repository instance scoped to the current tenant</returns>
        Task<IRepository<T>> CreateRepositoryAsync<T>() where T : class, IDataModel;

        /// <summary>
        /// Gets or creates a cached repository instance for better performance.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>A cached repository instance</returns>
        Task<IRepository<T>> GetOrCreateRepositoryAsync<T>(Guid tenantId) where T : class, IDataModel;

        /// <summary>
        /// Clears the repository cache for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant ID to clear cache for</param>
        Task ClearCacheAsync(Guid tenantId);

        /// <summary>
        /// Clears all repository caches.
        /// </summary>
        Task ClearAllCachesAsync();
    }
}