using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Repository.Mongo;

/// <summary>
/// Manages MongoDB connection pooling for multi-tenant scenarios.
/// Provides efficient connection management with proper lifecycle handling.
/// </summary>
public class TenantConnectionPool : IDisposable
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<TenantConnectionPool> _logger;
    private readonly MultiTenancyOptions _options;
    
    // Connection pool with tenant isolation
    private readonly ConcurrentDictionary<Guid, TenantConnectionInfo> _connectionPool = new();
    private readonly SemaphoreSlim _poolLock = new(1, 1);
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _connectionIdleTimeout;
    private readonly int _maxConnectionsPerTenant;
    private bool _disposed;

    public TenantConnectionPool(
        ITenantConnectionProvider connectionProvider,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantConnectionPool> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Configure pool settings
        _connectionIdleTimeout = TimeSpan.FromMinutes(
            _options.AdditionalSettings.TryGetValue("ConnectionIdleTimeoutMinutes", out var timeout) 
                ? Convert.ToInt32(timeout) 
                : 30);
                
        _maxConnectionsPerTenant = _options.AdditionalSettings.TryGetValue("MaxConnectionsPerTenant", out var maxConn) 
            ? Convert.ToInt32(maxConn) 
            : 100;
        
        // Start cleanup timer
        _cleanupTimer = new Timer(
            CleanupIdleConnections, 
            null, 
            TimeSpan.FromMinutes(5), 
            TimeSpan.FromMinutes(5));
            
        _logger.LogInformation(
            "Initialized tenant connection pool with idle timeout {Timeout} minutes and max {MaxConnections} connections per tenant",
            _connectionIdleTimeout.TotalMinutes, _maxConnectionsPerTenant);
    }

    /// <summary>
    /// Gets a MongoDB database connection for the specified tenant.
    /// </summary>
    public async Task<IMongoDatabase> GetDatabaseAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        // Try to get existing connection
        if (_connectionPool.TryGetValue(tenantId, out var connectionInfo))
        {
            connectionInfo.LastAccessed = DateTime.UtcNow;
            connectionInfo.AccessCount++;
            
            if (connectionInfo.IsHealthy)
            {
                return connectionInfo.Database;
            }
            
            _logger.LogWarning(
                "Existing connection for tenant {TenantId} is unhealthy, creating new connection",
                tenantId);
        }

        // Create new connection
        await _poolLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_connectionPool.TryGetValue(tenantId, out connectionInfo) && connectionInfo.IsHealthy)
            {
                connectionInfo.LastAccessed = DateTime.UtcNow;
                connectionInfo.AccessCount++;
                return connectionInfo.Database;
            }

            // Remove unhealthy connection if exists
            if (connectionInfo != null)
            {
                _connectionPool.TryRemove(tenantId, out _);
                connectionInfo.Dispose();
            }

            // Create new connection
            var database = await CreateTenantConnectionAsync(tenantId, cancellationToken);
            
            connectionInfo = new TenantConnectionInfo
            {
                TenantId = tenantId,
                Database = database,
                Client = database.Client as MongoClient,
                Created = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 1
            };
            
            _connectionPool[tenantId] = connectionInfo;
            
            _logger.LogInformation(
                "Created new database connection for tenant {TenantId}. Pool size: {PoolSize}",
                tenantId, _connectionPool.Count);
            
            return database;
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// Gets statistics about the connection pool.
    /// </summary>
    public ConnectionPoolStats GetStats()
    {
        return new ConnectionPoolStats
        {
            TotalConnections = _connectionPool.Count,
            ActiveConnections = _connectionPool.Count(kvp => kvp.Value.IsHealthy),
            ConnectionsByTenant = _connectionPool
                .Select(kvp => new TenantConnectionStats
                {
                    TenantId = kvp.Key,
                    IsHealthy = kvp.Value.IsHealthy,
                    Created = kvp.Value.Created,
                    LastAccessed = kvp.Value.LastAccessed,
                    AccessCount = kvp.Value.AccessCount
                })
                .ToList()
        };
    }

    /// <summary>
    /// Evicts a specific tenant's connection from the pool.
    /// </summary>
    public async Task EvictTenantConnectionAsync(Guid tenantId)
    {
        await _poolLock.WaitAsync();
        try
        {
            if (_connectionPool.TryRemove(tenantId, out var connectionInfo))
            {
                connectionInfo.Dispose();
                _logger.LogInformation("Evicted connection for tenant {TenantId} from pool", tenantId);
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// Creates a new database connection for a tenant.
    /// </summary>
    private async Task<IMongoDatabase> CreateTenantConnectionAsync(
        Guid tenantId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Get tenant-specific connection options
            var options = await _connectionProvider.GetDatabaseConnectionAsync(tenantId, cancellationToken);
            
            // Configure MongoDB client settings for pooling
            var clientSettings = MongoClientSettings.FromConnectionString(options.MongoConnectionString);
            clientSettings.MaxConnectionPoolSize = _maxConnectionsPerTenant;
            clientSettings.MinConnectionPoolSize = 1;
            clientSettings.DirectConnection = false; // Allow automatic discovery
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
            
            // Create client and get database
            var client = new MongoClient(clientSettings);
            var database = client.GetDatabase(options.DatabaseName);
            
            // Verify connection with a ping
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), 
                cancellationToken: cancellationToken);
            
            return database;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Periodically cleans up idle connections.
    /// </summary>
    private void CleanupIdleConnections(object? state)
    {
        if (_disposed) return;
        
        var cutoffTime = DateTime.UtcNow - _connectionIdleTimeout;
        var tenantsToEvict = _connectionPool
            .Where(kvp => kvp.Value.LastAccessed < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        if (tenantsToEvict.Any())
        {
            _logger.LogInformation(
                "Cleaning up {Count} idle tenant connections",
                tenantsToEvict.Count);

            foreach (var tenantId in tenantsToEvict)
            {
                _ = Task.Run(async () => await EvictTenantConnectionAsync(tenantId));
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantConnectionPool));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _cleanupTimer?.Dispose();
        
        // Dispose all connections
        foreach (var connectionInfo in _connectionPool.Values)
        {
            connectionInfo.Dispose();
        }
        
        _connectionPool.Clear();
        _poolLock?.Dispose();
        
        _logger.LogInformation("Tenant connection pool disposed");
    }

    /// <summary>
    /// Information about a tenant's database connection.
    /// </summary>
    private class TenantConnectionInfo : IDisposable
    {
        public Guid TenantId { get; set; }
        public IMongoDatabase Database { get; set; } = null!;
        public MongoClient? Client { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastAccessed { get; set; }
        public long AccessCount { get; set; }
        
        public bool IsHealthy
        {
            get
            {
                try
                {
                    // Quick health check - verify cluster is reachable
                    var cluster = Client?.Cluster;
                    return cluster != null && cluster.Description.State == MongoDB.Driver.Core.Clusters.ClusterState.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }
        
        public void Dispose()
        {
            // MongoDB client manages its own connection pooling
            // We don't need to explicitly dispose it
        }
    }
}

/// <summary>
/// Statistics about the connection pool.
/// </summary>
public class ConnectionPoolStats
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public List<TenantConnectionStats> ConnectionsByTenant { get; set; } = new();
}

/// <summary>
/// Statistics about a specific tenant's connection.
/// </summary>
public class TenantConnectionStats
{
    public Guid TenantId { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastAccessed { get; set; }
    public long AccessCount { get; set; }
}