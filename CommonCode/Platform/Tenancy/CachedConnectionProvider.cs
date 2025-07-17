using BFormDomain.Mongo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Caching wrapper around ITenantConnectionProvider that caches connections in memory.
/// Improves performance by avoiding repeated database lookups for connection information.
/// </summary>
public class CachedConnectionProvider : ITenantConnectionProvider
{
    private readonly ITenantConnectionProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedConnectionProvider> _logger;
    private readonly MultiTenancyOptions _options;
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<Guid, CacheMetrics> _cacheMetrics = new();
    private readonly Timer _metricsTimer;

    public CachedConnectionProvider(
        ITenantConnectionProvider innerProvider,
        IMemoryCache cache,
        IOptions<MultiTenancyOptions> options,
        ILogger<CachedConnectionProvider> logger)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Configure cache duration from options or use default
        _cacheDuration = TimeSpan.FromMinutes(_options.ConnectionCacheDurationMinutes);
        
        // Start metrics collection timer
        _metricsTimer = new Timer(
            LogCacheMetrics, 
            null, 
            TimeSpan.FromMinutes(5), 
            TimeSpan.FromMinutes(5));
            
        _logger.LogInformation(
            "Initialized cached connection provider with {Duration} minute cache duration",
            _cacheDuration.TotalMinutes);
    }

    public async Task<MongoRepositoryOptions> GetDatabaseConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"db_connection_{tenantId}";
        var metrics = GetOrCreateMetrics(tenantId);
        
        if (_cache.TryGetValue(cacheKey, out MongoRepositoryOptions? cachedOptions) && cachedOptions != null)
        {
            metrics.DatabaseHits++;
            metrics.LastAccessTime = DateTime.UtcNow;
            _logger.LogDebug("Retrieved database connection for tenant {TenantId} from cache (hit rate: {HitRate:P})", 
                tenantId, metrics.DatabaseHitRate);
            return cachedOptions;
        }

        metrics.DatabaseMisses++;
        _logger.LogDebug("Database connection for tenant {TenantId} not in cache, fetching from provider", tenantId);
        
        var startTime = DateTime.UtcNow;
        var options = await _innerProvider.GetDatabaseConnectionAsync(tenantId, cancellationToken);
        var fetchDuration = DateTime.UtcNow - startTime;
        
        // Determine cache priority based on access patterns
        var priority = DetermineCachePriority(metrics);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AdjustCacheDuration(metrics),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = priority
        };
        
        // Register eviction callback for metrics
        cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            _logger.LogDebug("Cache entry {Key} evicted. Reason: {Reason}", key, reason);
            if (state is CacheMetrics m)
            {
                m.LastEvictionReason = reason.ToString();
                m.LastEvictionTime = DateTime.UtcNow;
            }
        }, metrics);
        
        _cache.Set(cacheKey, options, cacheEntryOptions);
        metrics.AverageFetchTime = ((metrics.AverageFetchTime * (metrics.DatabaseMisses - 1)) + fetchDuration.TotalMilliseconds) / metrics.DatabaseMisses;
        
        _logger.LogDebug("Cached database connection for tenant {TenantId} (fetch time: {FetchTime}ms, priority: {Priority})", 
            tenantId, fetchDuration.TotalMilliseconds, priority);
        
        return options;
    }

    public async Task<StorageConnectionOptions> GetStorageConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"storage_connection_{tenantId}";
        var metrics = GetOrCreateMetrics(tenantId);
        
        if (_cache.TryGetValue(cacheKey, out StorageConnectionOptions? cachedOptions) && cachedOptions != null)
        {
            metrics.StorageHits++;
            metrics.LastAccessTime = DateTime.UtcNow;
            _logger.LogDebug("Retrieved storage connection for tenant {TenantId} from cache (hit rate: {HitRate:P})", 
                tenantId, metrics.StorageHitRate);
            return cachedOptions;
        }

        metrics.StorageMisses++;
        _logger.LogDebug("Storage connection for tenant {TenantId} not in cache, fetching from provider", tenantId);
        
        var startTime = DateTime.UtcNow;
        var options = await _innerProvider.GetStorageConnectionAsync(tenantId, cancellationToken);
        var fetchDuration = DateTime.UtcNow - startTime;
        
        // Determine cache priority based on access patterns
        var priority = DetermineCachePriority(metrics);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AdjustCacheDuration(metrics),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = priority
        };
        
        // Register eviction callback for metrics
        cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            _logger.LogDebug("Cache entry {Key} evicted. Reason: {Reason}", key, reason);
            if (state is CacheMetrics m)
            {
                m.LastEvictionReason = reason.ToString();
                m.LastEvictionTime = DateTime.UtcNow;
            }
        }, metrics);
        
        _cache.Set(cacheKey, options, cacheEntryOptions);
        
        _logger.LogDebug("Cached storage connection for tenant {TenantId} (fetch time: {FetchTime}ms, priority: {Priority})", 
            tenantId, fetchDuration.TotalMilliseconds, priority);
        
        return options;
    }

    public async Task RefreshConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing cached connections for tenant {TenantId}", tenantId);
        
        // Remove from cache
        var dbCacheKey = $"db_connection_{tenantId}";
        var storageCacheKey = $"storage_connection_{tenantId}";
        
        _cache.Remove(dbCacheKey);
        _cache.Remove(storageCacheKey);
        
        // Refresh underlying provider
        await _innerProvider.RefreshConnectionsAsync(tenantId, cancellationToken);
        
        _logger.LogDebug("Cleared cached connections for tenant {TenantId}", tenantId);
    }

    public async Task<Dictionary<ConnectionType, bool>> TestConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Connection testing should not be cached - always delegate to inner provider
        _logger.LogDebug("Testing connections for tenant {TenantId}", tenantId);
        return await _innerProvider.TestConnectionAsync(tenantId, cancellationToken);
    }
    
    /// <summary>
    /// Gets or creates cache metrics for a tenant.
    /// </summary>
    private CacheMetrics GetOrCreateMetrics(Guid tenantId)
    {
        return _cacheMetrics.GetOrAdd(tenantId, _ => new CacheMetrics { TenantId = tenantId });
    }
    
    /// <summary>
    /// Determines cache priority based on access patterns.
    /// </summary>
    private CacheItemPriority DetermineCachePriority(CacheMetrics metrics)
    {
        // High-frequency access tenants get higher priority
        var totalAccess = metrics.DatabaseHits + metrics.DatabaseMisses + metrics.StorageHits + metrics.StorageMisses;
        
        if (totalAccess > 1000 || metrics.DatabaseHitRate > 0.9)
        {
            return CacheItemPriority.NeverRemove;
        }
        else if (totalAccess > 100 || metrics.DatabaseHitRate > 0.7)
        {
            return CacheItemPriority.High;
        }
        else if (totalAccess > 10)
        {
            return CacheItemPriority.Normal;
        }
        else
        {
            return CacheItemPriority.Low;
        }
    }
    
    /// <summary>
    /// Adjusts cache duration based on access patterns.
    /// </summary>
    private TimeSpan AdjustCacheDuration(CacheMetrics metrics)
    {
        // Frequently accessed tenants get longer cache duration
        if (metrics.DatabaseHitRate > 0.8)
        {
            return _cacheDuration.Add(TimeSpan.FromMinutes(15)); // Extended duration
        }
        else if (metrics.DatabaseHitRate < 0.3)
        {
            return TimeSpan.FromMinutes(Math.Max(5, _cacheDuration.TotalMinutes / 2)); // Reduced duration
        }
        
        return _cacheDuration; // Default duration
    }
    
    /// <summary>
    /// Logs cache metrics periodically.
    /// </summary>
    private void LogCacheMetrics(object? state)
    {
        if (!_cacheMetrics.Any()) return;
        
        var totalHits = _cacheMetrics.Values.Sum(m => m.DatabaseHits + m.StorageHits);
        var totalMisses = _cacheMetrics.Values.Sum(m => m.DatabaseMisses + m.StorageMisses);
        var overallHitRate = totalHits + totalMisses > 0 ? (double)totalHits / (totalHits + totalMisses) : 0;
        
        _logger.LogInformation(
            "Cache metrics: Tenants={TenantCount}, Overall hit rate={HitRate:P}, Total hits={Hits}, Total misses={Misses}",
            _cacheMetrics.Count, overallHitRate, totalHits, totalMisses);
            
        // Log top 5 most active tenants
        var topTenants = _cacheMetrics
            .OrderByDescending(kvp => kvp.Value.DatabaseHits + kvp.Value.StorageHits)
            .Take(5)
            .Select(kvp => new { kvp.Key, kvp.Value.DatabaseHitRate, TotalAccess = kvp.Value.DatabaseHits + kvp.Value.DatabaseMisses });
            
        foreach (var tenant in topTenants)
        {
            _logger.LogDebug(
                "Top tenant {TenantId}: Hit rate={HitRate:P}, Total access={TotalAccess}",
                tenant.Key, tenant.DatabaseHitRate, tenant.TotalAccess);
        }
    }
    
    /// <summary>
    /// Cache metrics for a specific tenant.
    /// </summary>
    private class CacheMetrics
    {
        public Guid TenantId { get; set; }
        public long DatabaseHits { get; set; }
        public long DatabaseMisses { get; set; }
        public long StorageHits { get; set; }
        public long StorageMisses { get; set; }
        public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;
        public double AverageFetchTime { get; set; } // in milliseconds
        public string? LastEvictionReason { get; set; }
        public DateTime? LastEvictionTime { get; set; }
        
        public double DatabaseHitRate => DatabaseHits + DatabaseMisses > 0 
            ? (double)DatabaseHits / (DatabaseHits + DatabaseMisses) 
            : 0;
            
        public double StorageHitRate => StorageHits + StorageMisses > 0 
            ? (double)StorageHits / (StorageHits + StorageMisses) 
            : 0;
    }
}