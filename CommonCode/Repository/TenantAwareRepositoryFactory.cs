using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.CommonCode.Authorization;
using BFormDomain.DataModels;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Repository
{
    /// <summary>
    /// Factory for creating tenant-specific repository instances with caching support.
    /// </summary>
    public class TenantAwareRepositoryFactory : ITenantAwareRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITenantContext _tenantContext;
        private readonly ITenantConnectionProvider _connectionProvider;
        private readonly IOptions<MultiTenancyOptions> _multiTenancyOptions;
        private readonly SimpleApplicationAlert _alerts;
        private readonly ILoggerFactory _loggerFactory;
        
        // Cache repositories by type and tenant ID
        private readonly ConcurrentDictionary<string, object> _repositoryCache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);

        public TenantAwareRepositoryFactory(
            IServiceProvider serviceProvider,
            ITenantContext tenantContext,
            ITenantConnectionProvider connectionProvider,
            IOptions<MultiTenancyOptions> multiTenancyOptions,
            SimpleApplicationAlert alerts,
            ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _multiTenancyOptions = multiTenancyOptions ?? throw new ArgumentNullException(nameof(multiTenancyOptions));
            _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task<IRepository<T>> CreateRepositoryAsync<T>(Guid tenantId) where T : class, IDataModel
        {
            // Create a tenant-specific context
            var tenantSpecificContext = new TenantSpecificContext(tenantId, _tenantContext);
            
            // Try to get the repository type from DI container
            var repositoryType = GetRepositoryType<T>();
            
            if (repositoryType != null && typeof(TenantAwareMongoRepository<T>).IsAssignableFrom(repositoryType))
            {
                // Create instance with tenant-specific context
                var logger = _loggerFactory.CreateLogger(repositoryType);
                var instance = Activator.CreateInstance(
                    repositoryType,
                    tenantSpecificContext,
                    _connectionProvider,
                    _multiTenancyOptions,
                    _alerts,
                    logger) as IRepository<T>;
                
                if (instance != null)
                {
                    return instance;
                }
            }
            
            // Fallback to generic tenant-aware repository
            return new GenericTenantAwareRepository<T>(
                tenantSpecificContext,
                _connectionProvider,
                _multiTenancyOptions,
                _alerts,
                _loggerFactory.CreateLogger<GenericTenantAwareRepository<T>>());
        }

        public async Task<IRepository<T>> CreateRepositoryAsync<T>() where T : class, IDataModel
        {
            var tenantId = _tenantContext.CurrentTenantId ?? _multiTenancyOptions.Value.GlobalTenantId;
            return await CreateRepositoryAsync<T>(tenantId);
        }

        public async Task<IRepository<T>> GetOrCreateRepositoryAsync<T>(Guid tenantId) where T : class, IDataModel
        {
            var cacheKey = GetCacheKey<T>(tenantId);
            
            // Try to get from cache first
            if (_repositoryCache.TryGetValue(cacheKey, out var cached) && cached is IRepository<T> typedRepo)
            {
                return typedRepo;
            }
            
            // Create new instance with locking to prevent duplicate creation
            await _cacheLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_repositoryCache.TryGetValue(cacheKey, out cached) && cached is IRepository<T> typedRepo2)
                {
                    return typedRepo2;
                }
                
                // Create new repository
                var repository = await CreateRepositoryAsync<T>(tenantId);
                _repositoryCache[cacheKey] = repository;
                return repository;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task ClearCacheAsync(Guid tenantId)
        {
            await _cacheLock.WaitAsync();
            try
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in _repositoryCache)
                {
                    if (kvp.Key.EndsWith($"_{tenantId}"))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    if (_repositoryCache.TryRemove(key, out var repository) && repository is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task ClearAllCachesAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                foreach (var repository in _repositoryCache.Values)
                {
                    if (repository is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _repositoryCache.Clear();
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private Type? GetRepositoryType<T>() where T : class, IDataModel
        {
            // Try to resolve the specific repository type from DI
            var repository = _serviceProvider.GetService<IRepository<T>>();
            return repository?.GetType();
        }

        private string GetCacheKey<T>(Guid tenantId) where T : class, IDataModel
        {
            return $"{typeof(T).FullName}_{tenantId}";
        }

        /// <summary>
        /// Internal class to provide tenant-specific context
        /// </summary>
        private class TenantSpecificContext : ITenantContext
        {
            private readonly Guid _tenantId;
            private readonly ITenantContext _originalContext;

            public TenantSpecificContext(Guid tenantId, ITenantContext originalContext)
            {
                _tenantId = tenantId;
                _originalContext = originalContext;
            }

            public Guid? CurrentTenantId => _tenantId;
            public string? TenantId => _tenantId.ToString();
            public ApplicationUser? CurrentUser => _originalContext.CurrentUser;
            public bool IsRootUser => _originalContext.IsRootUser;
            public bool IsMultiTenancyEnabled => _originalContext.IsMultiTenancyEnabled;

            public bool HasAccessToTenant(Guid tenantId)
            {
                // In factory context, we assume access is validated before creation
                return tenantId == _tenantId || IsRootUser;
            }

            public void SetCurrentTenant(Guid? tenantId)
            {
                // Not supported in factory context
                throw new NotSupportedException("Cannot change tenant in factory-created context");
            }

            public void SetCurrentUser(ApplicationUser? user)
            {
                // Not supported in factory context
                throw new NotSupportedException("Cannot change user in factory-created context");
            }
        }

        /// <summary>
        /// Generic tenant-aware repository for entities without specific repository implementations
        /// </summary>
        private class GenericTenantAwareRepository<T> : TenantAwareMongoRepository<T> where T : class, IDataModel
        {
            private readonly string _collectionName;

            public GenericTenantAwareRepository(
                ITenantContext tenantContext,
                ITenantConnectionProvider connectionProvider,
                IOptions<MultiTenancyOptions> multiTenancyOptions,
                SimpleApplicationAlert alerts,
                ILogger<GenericTenantAwareRepository<T>>? logger = null)
                : base(tenantContext, connectionProvider, multiTenancyOptions, alerts, logger)
            {
                _collectionName = typeof(T).Name;
            }

            protected override string CollectionName => _collectionName;
        }
    }
}