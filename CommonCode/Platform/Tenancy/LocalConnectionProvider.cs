using BFormDomain.Mongo;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Local connection provider that uses configuration-based connections for development.
/// Falls back to default connections when tenant-specific connections are not configured.
/// </summary>
public class LocalConnectionProvider : ITenantConnectionProvider
{
    private readonly TenantConnectionRepository _connectionRepository;
    private readonly IOptions<MongoRepositoryOptions> _defaultDatabaseOptions;
    private readonly IOptions<StorageConnectionOptions> _defaultStorageOptions;
    private readonly ILogger<LocalConnectionProvider> _logger;

    public LocalConnectionProvider(
        TenantConnectionRepository connectionRepository,
        IOptions<MongoRepositoryOptions> defaultDatabaseOptions,
        IOptions<StorageConnectionOptions> defaultStorageOptions,
        ILogger<LocalConnectionProvider> logger)
    {
        _connectionRepository = connectionRepository;
        _defaultDatabaseOptions = defaultDatabaseOptions;
        _defaultStorageOptions = defaultStorageOptions;
        _logger = logger;
    }

    public async Task<MongoRepositoryOptions> GetDatabaseConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByTenantAndTypeAsync(tenantId, ConnectionType.Database, cancellationToken);
        
        if (connection == null)
        {
            _logger.LogDebug("No database connection found for tenant {TenantId}, using default", tenantId);
            
            // Use default connection with tenant-specific database name
            var options = new MongoRepositoryOptions
            {
                MongoConnectionString = _defaultDatabaseOptions.Value.MongoConnectionString,
                DatabaseName = $"{_defaultDatabaseOptions.Value.DatabaseName}_{tenantId:N}",
                
                // Copy all other settings from default
                DefaultPageSize = _defaultDatabaseOptions.Value.DefaultPageSize,
                FaultLimit = _defaultDatabaseOptions.Value.FaultLimit,
                MaxConnectionPoolSize = _defaultDatabaseOptions.Value.MaxConnectionPoolSize,
                MinConnectionPoolSize = _defaultDatabaseOptions.Value.MinConnectionPoolSize,
                WaitQueueTimeoutMs = _defaultDatabaseOptions.Value.WaitQueueTimeoutMs,
                ConnectionIdleTimeoutMs = _defaultDatabaseOptions.Value.ConnectionIdleTimeoutMs,
                ConnectionLifetimeMs = _defaultDatabaseOptions.Value.ConnectionLifetimeMs,
                CommandTimeoutMs = _defaultDatabaseOptions.Value.CommandTimeoutMs,
                SocketTimeoutMs = _defaultDatabaseOptions.Value.SocketTimeoutMs,
                EnableQueryLogging = _defaultDatabaseOptions.Value.EnableQueryLogging,
                SlowQueryThresholdMs = _defaultDatabaseOptions.Value.SlowQueryThresholdMs,
                UseSsl = _defaultDatabaseOptions.Value.UseSsl,
                MaxRetryAttempts = _defaultDatabaseOptions.Value.MaxRetryAttempts,
                RetryDelayMs = _defaultDatabaseOptions.Value.RetryDelayMs,
                RetryBackoffMultiplier = _defaultDatabaseOptions.Value.RetryBackoffMultiplier,
                MaxRetryDelayMs = _defaultDatabaseOptions.Value.MaxRetryDelayMs,
                ReadPreference = _defaultDatabaseOptions.Value.ReadPreference,
                MaxStalenessSeconds = _defaultDatabaseOptions.Value.MaxStalenessSeconds,
                WriteConcern = _defaultDatabaseOptions.Value.WriteConcern,
                WriteConcernTimeoutMs = _defaultDatabaseOptions.Value.WriteConcernTimeoutMs,
                EnablePerformanceCounters = _defaultDatabaseOptions.Value.EnablePerformanceCounters,
                EnableServerMonitoring = _defaultDatabaseOptions.Value.EnableServerMonitoring,
                HeartbeatIntervalMs = _defaultDatabaseOptions.Value.HeartbeatIntervalMs,
                BulkWriteBatchSize = _defaultDatabaseOptions.Value.BulkWriteBatchSize,
                BulkWriteOrdered = _defaultDatabaseOptions.Value.BulkWriteOrdered,
                EnableIndexHints = _defaultDatabaseOptions.Value.EnableIndexHints,
                IndexHints = new Dictionary<string, string>(_defaultDatabaseOptions.Value.IndexHints),
                EnableRetryPolicy = _defaultDatabaseOptions.Value.EnableRetryPolicy,
                MaxRetryCount = _defaultDatabaseOptions.Value.MaxRetryCount,
                CircuitBreakerThreshold = _defaultDatabaseOptions.Value.CircuitBreakerThreshold,
                CircuitBreakerDurationSeconds = _defaultDatabaseOptions.Value.CircuitBreakerDurationSeconds,
                AutoCreateIndexes = _defaultDatabaseOptions.Value.AutoCreateIndexes
            };
            
            return options;
        }

        _logger.LogDebug("Found database connection for tenant {TenantId}", tenantId);
        
        // TODO: Decrypt connection string and create options from tenant connection
        // For now, use default configuration with tenant-specific database name
        var tenantOptions = new MongoRepositoryOptions
        {
            MongoConnectionString = DecryptConnectionString(connection.EncryptedConnectionString),
            DatabaseName = connection.DatabaseName ?? $"{_defaultDatabaseOptions.Value.DatabaseName}_{tenantId:N}",
            
            // Copy default settings
            DefaultPageSize = _defaultDatabaseOptions.Value.DefaultPageSize,
            FaultLimit = _defaultDatabaseOptions.Value.FaultLimit,
            MaxConnectionPoolSize = _defaultDatabaseOptions.Value.MaxConnectionPoolSize,
            MinConnectionPoolSize = _defaultDatabaseOptions.Value.MinConnectionPoolSize,
            WaitQueueTimeoutMs = _defaultDatabaseOptions.Value.WaitQueueTimeoutMs,
            ConnectionIdleTimeoutMs = _defaultDatabaseOptions.Value.ConnectionIdleTimeoutMs,
            ConnectionLifetimeMs = _defaultDatabaseOptions.Value.ConnectionLifetimeMs,
            CommandTimeoutMs = _defaultDatabaseOptions.Value.CommandTimeoutMs,
            SocketTimeoutMs = _defaultDatabaseOptions.Value.SocketTimeoutMs,
            EnableQueryLogging = _defaultDatabaseOptions.Value.EnableQueryLogging,
            SlowQueryThresholdMs = _defaultDatabaseOptions.Value.SlowQueryThresholdMs,
            UseSsl = _defaultDatabaseOptions.Value.UseSsl,
            MaxRetryAttempts = _defaultDatabaseOptions.Value.MaxRetryAttempts,
            RetryDelayMs = _defaultDatabaseOptions.Value.RetryDelayMs,
            RetryBackoffMultiplier = _defaultDatabaseOptions.Value.RetryBackoffMultiplier,
            MaxRetryDelayMs = _defaultDatabaseOptions.Value.MaxRetryDelayMs,
            ReadPreference = _defaultDatabaseOptions.Value.ReadPreference,
            MaxStalenessSeconds = _defaultDatabaseOptions.Value.MaxStalenessSeconds,
            WriteConcern = _defaultDatabaseOptions.Value.WriteConcern,
            WriteConcernTimeoutMs = _defaultDatabaseOptions.Value.WriteConcernTimeoutMs,
            EnablePerformanceCounters = _defaultDatabaseOptions.Value.EnablePerformanceCounters,
            EnableServerMonitoring = _defaultDatabaseOptions.Value.EnableServerMonitoring,
            HeartbeatIntervalMs = _defaultDatabaseOptions.Value.HeartbeatIntervalMs,
            BulkWriteBatchSize = _defaultDatabaseOptions.Value.BulkWriteBatchSize,
            BulkWriteOrdered = _defaultDatabaseOptions.Value.BulkWriteOrdered,
            EnableIndexHints = _defaultDatabaseOptions.Value.EnableIndexHints,
            IndexHints = new Dictionary<string, string>(_defaultDatabaseOptions.Value.IndexHints),
            EnableRetryPolicy = _defaultDatabaseOptions.Value.EnableRetryPolicy,
            MaxRetryCount = _defaultDatabaseOptions.Value.MaxRetryCount,
            CircuitBreakerThreshold = _defaultDatabaseOptions.Value.CircuitBreakerThreshold,
            CircuitBreakerDurationSeconds = _defaultDatabaseOptions.Value.CircuitBreakerDurationSeconds,
            AutoCreateIndexes = _defaultDatabaseOptions.Value.AutoCreateIndexes
        };

        return tenantOptions;
    }

    public async Task<StorageConnectionOptions> GetStorageConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByTenantAndTypeAsync(tenantId, ConnectionType.Storage, cancellationToken);
        
        if (connection == null)
        {
            _logger.LogDebug("No storage connection found for tenant {TenantId}, using default", tenantId);
            
            // Use default storage with tenant-specific path
            var options = new StorageConnectionOptions
            {
                Provider = _defaultStorageOptions.Value.Provider,
                ConnectionString = _defaultStorageOptions.Value.ConnectionString,
                BasePath = $"{_defaultStorageOptions.Value.BasePath}/tenant_{tenantId:N}",
                UseManagedIdentity = _defaultStorageOptions.Value.UseManagedIdentity,
                ServiceEndpoint = _defaultStorageOptions.Value.ServiceEndpoint,
                DefaultAccessTier = _defaultStorageOptions.Value.DefaultAccessTier,
                EnableVersioning = _defaultStorageOptions.Value.EnableVersioning,
                MaxVersionCount = _defaultStorageOptions.Value.MaxVersionCount,
                EnableSoftDelete = _defaultStorageOptions.Value.EnableSoftDelete,
                SoftDeleteRetentionDays = _defaultStorageOptions.Value.SoftDeleteRetentionDays,
                RequestTimeoutSeconds = _defaultStorageOptions.Value.RequestTimeoutSeconds,
                MaxRetryAttempts = _defaultStorageOptions.Value.MaxRetryAttempts,
                EnableCaching = _defaultStorageOptions.Value.EnableCaching,
                CacheExpirationMinutes = _defaultStorageOptions.Value.CacheExpirationMinutes,
                AdditionalSettings = new Dictionary<string, string>(_defaultStorageOptions.Value.AdditionalSettings)
            };
            
            return options;
        }

        _logger.LogDebug("Found storage connection for tenant {TenantId}", tenantId);
        
        // Create storage options from tenant connection
        var tenantOptions = new StorageConnectionOptions
        {
            Provider = connection.Provider,
            ConnectionString = DecryptConnectionString(connection.EncryptedConnectionString),
            BasePath = connection.ContainerPrefix,
            UseManagedIdentity = _defaultStorageOptions.Value.UseManagedIdentity,
            ServiceEndpoint = _defaultStorageOptions.Value.ServiceEndpoint,
            DefaultAccessTier = _defaultStorageOptions.Value.DefaultAccessTier,
            EnableVersioning = _defaultStorageOptions.Value.EnableVersioning,
            MaxVersionCount = _defaultStorageOptions.Value.MaxVersionCount,
            EnableSoftDelete = _defaultStorageOptions.Value.EnableSoftDelete,
            SoftDeleteRetentionDays = _defaultStorageOptions.Value.SoftDeleteRetentionDays,
            RequestTimeoutSeconds = _defaultStorageOptions.Value.RequestTimeoutSeconds,
            MaxRetryAttempts = _defaultStorageOptions.Value.MaxRetryAttempts,
            EnableCaching = _defaultStorageOptions.Value.EnableCaching,
            CacheExpirationMinutes = _defaultStorageOptions.Value.CacheExpirationMinutes,
            AdditionalSettings = new Dictionary<string, string>(connection.AdditionalSettings)
        };

        return tenantOptions;
    }

    public async Task RefreshConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing connections for tenant {TenantId}", tenantId);
        // Local provider doesn't cache, so nothing to refresh
        await Task.CompletedTask;
    }

    public async Task<Dictionary<ConnectionType, bool>> TestConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<ConnectionType, bool>();

        try
        {
            // Test database connection
            var dbOptions = await GetDatabaseConnectionAsync(tenantId, cancellationToken);
            var client = new MongoClient(dbOptions.MongoConnectionString);
            var database = client.GetDatabase(dbOptions.DatabaseName);
            await database.RunCommandAsync<object>("{ ping: 1 }", cancellationToken: cancellationToken);
            results[ConnectionType.Database] = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection test failed for tenant {TenantId}", tenantId);
            results[ConnectionType.Database] = false;
        }

        try
        {
            // Test storage connection (simplified test - just check if path exists for file system)
            var storageOptions = await GetStorageConnectionAsync(tenantId, cancellationToken);
            
            if (storageOptions.Provider == "FileSystem")
            {
                var basePath = storageOptions.BasePath ?? ".";
                results[ConnectionType.Storage] = Directory.Exists(basePath) || File.Exists(basePath);
            }
            else
            {
                // For other providers, assume true for now
                results[ConnectionType.Storage] = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage connection test failed for tenant {TenantId}", tenantId);
            results[ConnectionType.Storage] = false;
        }

        return results;
    }

    private string DecryptConnectionString(string encryptedConnectionString)
    {
        // TODO: Implement proper encryption/decryption
        // For Phase 1, assume connection strings are stored in plain text for simplicity
        return encryptedConnectionString;
    }
}