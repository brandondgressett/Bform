using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.DataModels;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Repository.Mongo
{
    /// <summary>
    /// Base repository class that provides tenant isolation through separate database connections.
    /// Each tenant has their own database, so no query filtering is needed.
    /// Write operations validate tenant ownership as a safety check.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement IDataModel</typeparam>
    public abstract class TenantAwareMongoRepository<T> : MongoRepository<T> where T : class, IDataModel
    {
        protected readonly ITenantContext _tenantContext;
        protected readonly ITenantConnectionProvider _connectionProvider;
        protected readonly MultiTenancyOptions _multiTenancyOptions;
        protected readonly ILogger<TenantAwareMongoRepository<T>>? _tenantLogger;

        // Connection pool is now managed centrally
        private readonly TenantConnectionPool? _connectionPool;

        protected TenantAwareMongoRepository(
            ITenantContext tenantContext,
            ITenantConnectionProvider connectionProvider,
            IOptions<MultiTenancyOptions> multiTenancyOptions,
            SimpleApplicationAlert alerts,
            ILogger<TenantAwareMongoRepository<T>>? logger = null,
            TenantConnectionPool? connectionPool = null)
            : base(GetFallbackOptions(multiTenancyOptions.Value), alerts, logger)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _multiTenancyOptions = multiTenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
            _tenantLogger = logger;
            _connectionPool = connectionPool;
        }

        /// <summary>
        /// Provides fallback options for base constructor. In multi-tenant mode, actual options
        /// come from the connection provider.
        /// </summary>
        private static IOptions<MongoRepositoryOptions> GetFallbackOptions(MultiTenancyOptions multiTenancyOptions)
        {
            // Return empty options - will be overridden by tenant-specific connection
            return Options.Create(new MongoRepositoryOptions
            {
                MongoConnectionString = "mongodb://localhost:27017",
                DatabaseName = "BFormDomain_Fallback"
            });
        }

        /// <summary>
        /// Override CreateCollection to use tenant-specific database
        /// </summary>
        protected override IMongoCollection<T> CreateCollection()
        {
            var database = GetTenantDatabase();
            var collection = database.GetCollection<T>(CollectionName);
            return collection;
        }

        /// <summary>
        /// Get tenant-specific database connection
        /// </summary>
        private IMongoDatabase GetTenantDatabase()
        {
            var tenantId = GetCurrentTenantId();

            // Use connection pool if available
            if (_connectionPool != null)
            {
                var task = _connectionPool.GetDatabaseAsync(tenantId);
                return task.GetAwaiter().GetResult();
            }

            // Fallback to direct connection
            var fallbackTask = GetTenantDatabaseDirectAsync(tenantId);
            return fallbackTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Direct database connection without pooling (fallback)
        /// </summary>
        private async Task<IMongoDatabase> GetTenantDatabaseDirectAsync(Guid tenantId)
        {
            // Get tenant-specific connection options
            var tenantOptions = await _connectionProvider.GetDatabaseConnectionAsync(tenantId);
            var mongoClient = MongoEnvironment.MakeClient(tenantOptions.MongoConnectionString, tenantOptions);
            var database = mongoClient.GetDatabase(tenantOptions.DatabaseName);

            _tenantLogger?.LogDebug(
                "Created direct database connection for tenant {TenantId} to database {DatabaseName}",
                tenantId, database.DatabaseNamespace.DatabaseName);

            return database;
        }

        /// <summary>
        /// Gets the current tenant ID, handling both multi-tenant and single-tenant modes
        /// </summary>
        protected virtual Guid GetCurrentTenantId()
        {
            if (!_multiTenancyOptions.Enabled)
            {
                // Single-tenant mode - use global tenant
                return _multiTenancyOptions.GlobalTenantId;
            }

            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "No tenant context available. Ensure TenantContextMiddleware is configured.");
            }

            return tenantId.Value;
        }


        /// <summary>
        /// Validates that an entity belongs to the current tenant
        /// </summary>
        protected virtual void ValidateTenantOwnership(T entity)
        {
            if (entity is ITenantScoped tenantScoped)
            {
                var currentTenantId = GetCurrentTenantId();
                
                if (tenantScoped.TenantId == Guid.Empty)
                {
                    // Set tenant ID if not set
                    tenantScoped.TenantId = currentTenantId;
                }
                else if (tenantScoped.TenantId != currentTenantId)
                {
                    // Prevent cross-tenant operations
                    throw new UnauthorizedAccessException(
                        $"Entity belongs to tenant {tenantScoped.TenantId} but current tenant is {currentTenantId}");
                }
            }
        }

        /// <summary>
        /// Validates a collection of entities for tenant ownership
        /// </summary>
        protected virtual void ValidateTenantOwnership(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                ValidateTenantOwnership(entity);
            }
        }


        #region Override Write Methods to Validate Tenant Ownership

        public override void Create(T newItem)
        {
            ValidateTenantOwnership(newItem);
            base.Create(newItem);
        }

        public override async Task CreateAsync(T newItem)
        {
            ValidateTenantOwnership(newItem);
            await base.CreateAsync(newItem);
        }

        public override async Task CreateBatchAsync(IEnumerable<T> newItems)
        {
            ValidateTenantOwnership(newItems);
            await base.CreateBatchAsync(newItems);
        }

        public override void Update((T, RepositoryContext) data)
        {
            ValidateTenantOwnership(data.Item1);
            base.Update(data);
        }

        public override void Update(T data)
        {
            ValidateTenantOwnership(data);
            base.Update(data);
        }

        public override async Task UpdateAsync((T, RepositoryContext) data)
        {
            ValidateTenantOwnership(data.Item1);
            await base.UpdateAsync(data);
        }

        public override async Task UpdateAsync(T data)
        {
            ValidateTenantOwnership(data);
            await base.UpdateAsync(data);
        }

        public override void Delete((T, RepositoryContext) data)
        {
            ValidateTenantOwnership(data.Item1);
            base.Delete(data);
        }

        public override void Delete(T doc)
        {
            ValidateTenantOwnership(doc);
            base.Delete(doc);
        }

        public override async Task DeleteAsync((T, RepositoryContext) data)
        {
            ValidateTenantOwnership(data.Item1);
            await base.DeleteAsync(data);
        }

        public override async Task DeleteAsync(T doc)
        {
            ValidateTenantOwnership(doc);
            await base.DeleteAsync(doc);
        }


        #endregion

        #region Helper Methods

        /// <summary>
        /// Clears the database cache for a specific tenant
        /// </summary>
        public async Task ClearTenantCacheAsync(Guid tenantId)
        {
            // If using connection pool, evict the tenant connection
            if (_connectionPool != null)
            {
                await _connectionPool.EvictTenantConnectionAsync(tenantId);
            }
            // Otherwise, no local cache to clear
        }

        /// <summary>
        /// Clears all cached database connections
        /// </summary>
        public async Task ClearAllCachesAsync()
        {
            // Connection pool manages its own lifecycle
            // Individual repositories don't need to clear all caches
            await Task.CompletedTask;
        }

        #endregion

        #region Cross-Tenant Operations (Super Admin Only)

        /// <summary>
        /// Executes a query across multiple tenants. Only available to super admins.
        /// Each tenant has their own database, so we query each database separately.
        /// </summary>
        protected async Task<List<T>> QueryAcrossTenantsAsync(
            IEnumerable<Guid> tenantIds,
            Expression<Func<T, bool>>? predicate = null)
        {
            if (!_tenantContext.IsRootUser)
            {
                throw new UnauthorizedAccessException("Cross-tenant queries require super admin privileges");
            }

            var results = new List<T>();
            var filter = predicate != null ? Builders<T>.Filter.Where(predicate) : Builders<T>.Filter.Empty;

            foreach (var tenantId in tenantIds)
            {
                // Get tenant-specific database options
                var tenantOptions = await _connectionProvider.GetDatabaseConnectionAsync(tenantId);
                var mongoClient = MongoEnvironment.MakeClient(tenantOptions.MongoConnectionString, tenantOptions);
                var database = mongoClient.GetDatabase(tenantOptions.DatabaseName);
                var collection = database.GetCollection<T>(CollectionName);

                // Query the tenant's database directly - no filtering needed
                var tenantResults = await collection.Find(filter).ToListAsync();
                results.AddRange(tenantResults);
            }

            return results;
        }

        #endregion

        public void Dispose()
        {
            // Connection pool manages its own lifecycle
            // Nothing to dispose at repository level
        }
    }
}