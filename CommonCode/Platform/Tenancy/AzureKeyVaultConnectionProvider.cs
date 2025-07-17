using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Connection provider that retrieves tenant connection strings from Azure Key Vault.
/// Provides secure storage and retrieval of sensitive connection information.
/// </summary>
public class AzureKeyVaultConnectionProvider : ITenantConnectionProvider
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultConnectionProvider> _logger;
    private readonly IMemoryCache _cache;
    private readonly AzureKeyVaultOptions _options;
    private readonly TenantConnectionRepository _tenantConnectionRepo;
    private readonly string _encryptionKey;

    public AzureKeyVaultConnectionProvider(
        IOptions<AzureKeyVaultOptions> options,
        TenantConnectionRepository tenantConnectionRepo,
        IMemoryCache cache,
        ILogger<AzureKeyVaultConnectionProvider> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _tenantConnectionRepo = tenantConnectionRepo ?? throw new ArgumentNullException(nameof(tenantConnectionRepo));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize Azure Key Vault client
        var credential = CreateCredential();
        _secretClient = new SecretClient(new Uri(_options.VaultUri), credential);
        
        // Get or create encryption key for local encryption
        _encryptionKey = GetOrCreateEncryptionKey();
    }

    /// <summary>
    /// Gets database connection options for a tenant, retrieving sensitive data from Key Vault.
    /// </summary>
    public async Task<MongoRepositoryOptions> GetDatabaseConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant_db_{tenantId}";
        
        if (_cache.TryGetValue<MongoRepositoryOptions>(cacheKey, out var cachedOptions))
        {
            _logger.LogDebug("Returning cached database connection for tenant {TenantId}", tenantId);
            return cachedOptions!;
        }

        _logger.LogInformation("Retrieving database connection for tenant {TenantId} from Key Vault", tenantId);

        try
        {
            // Get tenant connection metadata from database
            var connections = await _tenantConnectionRepo.GetByTenantIdAsync(tenantId, cancellationToken);
            var dbConnection = connections.FirstOrDefault(c => c.Type == ConnectionType.Database);
            
            if (dbConnection == null)
            {
                throw new InvalidOperationException($"No database connection found for tenant {tenantId}");
            }

            // Retrieve actual connection string from Key Vault
            var secretName = GetSecretName(tenantId, ConnectionType.Database);
            var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            
            var options = new MongoRepositoryOptions
            {
                MongoConnectionString = secret.Value.Value,
                DatabaseName = dbConnection.DatabaseName ?? $"tenant_{tenantId:N}",
                EnableRetryPolicy = true,
                MaxRetryCount = _options.DefaultMaxRetryCount,
                CircuitBreakerThreshold = _options.DefaultCircuitBreakerThreshold,
                CircuitBreakerDurationSeconds = _options.DefaultCircuitBreakerDurationSeconds
            };

            // Apply any additional settings from the connection metadata
            if (dbConnection.AdditionalSettings != null)
            {
                ApplyAdditionalSettings(options, dbConnection.AdditionalSettings);
            }

            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheDurationMinutes),
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(cacheKey, options, cacheEntryOptions);

            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve database connection for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets storage connection options for a tenant, retrieving sensitive data from Key Vault.
    /// </summary>
    public async Task<StorageConnectionOptions> GetStorageConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant_storage_{tenantId}";
        
        if (_cache.TryGetValue<StorageConnectionOptions>(cacheKey, out var cachedOptions))
        {
            _logger.LogDebug("Returning cached storage connection for tenant {TenantId}", tenantId);
            return cachedOptions!;
        }

        _logger.LogInformation("Retrieving storage connection for tenant {TenantId} from Key Vault", tenantId);

        try
        {
            // Get tenant connection metadata from database
            var connections = await _tenantConnectionRepo.GetByTenantIdAsync(tenantId, cancellationToken);
            var storageConnection = connections.FirstOrDefault(c => c.Type == ConnectionType.Storage);
            
            if (storageConnection == null)
            {
                // Use default storage settings if no specific storage connection
                return new StorageConnectionOptions
                {
                    Provider = "FileSystem",
                    BasePath = $"tenant_{tenantId:N}"
                };
            }

            var options = new StorageConnectionOptions
            {
                Provider = storageConnection.Provider,
                BasePath = storageConnection.ContainerPrefix ?? $"tenant_{tenantId:N}"
            };

            // For cloud storage providers, retrieve connection string from Key Vault
            if (options.Provider != "FileSystem")
            {
                var secretName = GetSecretName(tenantId, ConnectionType.Storage);
                var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                options.ConnectionString = secret.Value.Value;
            }

            // Apply any additional settings
            if (storageConnection.AdditionalSettings != null)
            {
                options.AdditionalSettings = storageConnection.AdditionalSettings;
            }

            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheDurationMinutes),
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(cacheKey, options, cacheEntryOptions);

            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve storage connection for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Refreshes cached connections for a specific tenant by clearing its cache entries.
    /// </summary>
    public Task RefreshConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing cached connections for tenant {TenantId}", tenantId);
        
        // Clear tenant-specific cache entries
        _cache.Remove($"tenant_db_{tenantId}");
        _cache.Remove($"tenant_storage_{tenantId}");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests connectivity for a tenant's connections.
    /// </summary>
    public async Task<Dictionary<ConnectionType, bool>> TestConnectionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<ConnectionType, bool>();
        
        // Test database connection
        try
        {
            var dbSecretName = GetSecretName(tenantId, ConnectionType.Database);
            var dbResponse = await _secretClient.GetSecretAsync(dbSecretName, cancellationToken: cancellationToken);
            results[ConnectionType.Database] = dbResponse.Value != null && !string.IsNullOrEmpty(dbResponse.Value.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection test failed for tenant {TenantId}", tenantId);
            results[ConnectionType.Database] = false;
        }
        
        // Test storage connection
        try
        {
            var storageSecretName = GetSecretName(tenantId, ConnectionType.Storage);
            var storageResponse = await _secretClient.GetSecretAsync(storageSecretName, cancellationToken: cancellationToken);
            results[ConnectionType.Storage] = storageResponse.Value != null && !string.IsNullOrEmpty(storageResponse.Value.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage connection test failed for tenant {TenantId}", tenantId);
            results[ConnectionType.Storage] = false;
        }
        
        return results;
    }

    /// <summary>
    /// Stores or updates a connection string in Key Vault.
    /// </summary>
    public async Task SaveConnectionAsync(Guid tenantId, ConnectionType type, string connectionString, CancellationToken cancellationToken = default)
    {
        var secretName = GetSecretName(tenantId, type);
        
        _logger.LogInformation("Saving {Type} connection for tenant {TenantId} to Key Vault", type, tenantId);
        
        try
        {
            var secret = new KeyVaultSecret(secretName, connectionString)
            {
                Properties =
                {
                    ExpiresOn = _options.SecretExpirationDays.HasValue 
                        ? DateTimeOffset.UtcNow.AddDays(_options.SecretExpirationDays.Value) 
                        : null,
                    Tags =
                    {
                        ["TenantId"] = tenantId.ToString(),
                        ["ConnectionType"] = type.ToString(),
                        ["CreatedDate"] = DateTime.UtcNow.ToString("O")
                    }
                }
            };

            await _secretClient.SetSecretAsync(secret, cancellationToken);
            
            // Clear cache for this tenant
            _cache.Remove($"tenant_db_{tenantId}");
            _cache.Remove($"tenant_storage_{tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Type} connection for tenant {TenantId}", type, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Creates the appropriate Azure credential based on configuration.
    /// </summary>
    private TokenCredential CreateCredential()
    {
        if (!string.IsNullOrEmpty(_options.TenantId) && 
            !string.IsNullOrEmpty(_options.ClientId) && 
            !string.IsNullOrEmpty(_options.ClientSecret))
        {
            // Use client secret credential for production
            return new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
        }
        else if (!string.IsNullOrEmpty(_options.ManagedIdentityClientId))
        {
            // Use managed identity with specific client ID
            return new ManagedIdentityCredential(_options.ManagedIdentityClientId);
        }
        else
        {
            // Use default Azure credential (works with Visual Studio, Azure CLI, etc.)
            return new DefaultAzureCredential();
        }
    }

    /// <summary>
    /// Generates a consistent secret name for a tenant and connection type.
    /// </summary>
    private string GetSecretName(Guid tenantId, ConnectionType type)
    {
        var prefix = _options.SecretNamePrefix ?? "tenant";
        return $"{prefix}-{tenantId:N}-{type.ToString().ToLowerInvariant()}";
    }

    /// <summary>
    /// Gets or creates an encryption key stored in Key Vault for local encryption needs.
    /// </summary>
    private string GetOrCreateEncryptionKey()
    {
        const string keySecretName = "bform-encryption-key";
        
        try
        {
            var response = _secretClient.GetSecret(keySecretName);
            return response.Value.Value;
        }
        catch
        {
            // Key doesn't exist, create a new one
            var newKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var secret = new KeyVaultSecret(keySecretName, newKey)
            {
                Properties =
                {
                    Tags =
                    {
                        ["Purpose"] = "BFormDomain local encryption",
                        ["CreatedDate"] = DateTime.UtcNow.ToString("O")
                    }
                }
            };
            
            _secretClient.SetSecret(secret);
            return newKey;
        }
    }

    /// <summary>
    /// Applies additional settings from the connection metadata to the options.
    /// </summary>
    private void ApplyAdditionalSettings(MongoRepositoryOptions options, Dictionary<string, string> settings)
    {
        foreach (var setting in settings)
        {
            switch (setting.Key.ToLowerInvariant())
            {
                case "maxretrycount":
                    if (int.TryParse(setting.Value, out var retryCount))
                        options.MaxRetryCount = retryCount;
                    break;
                    
                case "circuitbreakerthreshold":
                    if (int.TryParse(setting.Value, out var threshold))
                        options.CircuitBreakerThreshold = threshold;
                    break;
                    
                case "circuitbreakerdurationseconds":
                    if (int.TryParse(setting.Value, out var duration))
                        options.CircuitBreakerDurationSeconds = duration;
                    break;
                    
                case "enableretrypolicy":
                    if (bool.TryParse(setting.Value, out var enableRetry))
                        options.EnableRetryPolicy = enableRetry;
                    break;
            }
        }
    }
}

/// <summary>
/// Configuration options for Azure Key Vault integration.
/// </summary>
public class AzureKeyVaultOptions
{
    public const string SectionName = "AzureKeyVault";
    
    /// <summary>
    /// The URI of the Azure Key Vault (e.g., https://myvault.vault.azure.net/)
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure AD tenant ID for authentication (optional, for service principal auth)
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Client ID for service principal authentication (optional)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client secret for service principal authentication (optional)
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Managed identity client ID (optional, for managed identity auth)
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }
    
    /// <summary>
    /// Prefix for secret names in Key Vault
    /// </summary>
    public string SecretNamePrefix { get; set; } = "tenant";
    
    /// <summary>
    /// Cache duration for connection strings in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Optional expiration days for secrets
    /// </summary>
    public int? SecretExpirationDays { get; set; }
    
    /// <summary>
    /// Default retry settings
    /// </summary>
    public int DefaultMaxRetryCount { get; set; } = 3;
    public int DefaultCircuitBreakerThreshold { get; set; } = 5;
    public int DefaultCircuitBreakerDurationSeconds { get; set; } = 30;
}