using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Validates tenant configuration and connections before deployment or during runtime.
/// Ensures configurations are correct and connections are functional.
/// </summary>
public class TenantConfigurationValidator
{
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly TenantRepository _tenantRepository;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantConfigurationValidator> _logger;

    public TenantConfigurationValidator(
        ITenantConnectionProvider connectionProvider,
        TenantRepository tenantRepository,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantConfigurationValidator> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the multi-tenancy configuration.
    /// </summary>
    public async Task<ValidationResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        _logger.LogInformation("Starting multi-tenancy configuration validation");

        try
        {
            // 1. Validate basic configuration
            ValidateBasicConfiguration(result);

            // 2. Validate connection provider configuration
            await ValidateConnectionProviderAsync(result, cancellationToken);

            // 3. Validate global tenant configuration
            if (!_options.Enabled || _options.AutoCreateGlobalTenant)
            {
                await ValidateGlobalTenantAsync(result, cancellationToken);
            }

            // 4. Validate active tenants if in multi-tenant mode
            if (_options.Enabled)
            {
                await ValidateActiveTenantsAsync(result, cancellationToken);
            }

            result.IsValid = !result.Errors.Any();
            
            _logger.LogInformation(
                "Configuration validation completed. Valid: {IsValid}, Warnings: {WarningCount}, Errors: {ErrorCount}",
                result.IsValid, result.Warnings.Count, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed with exception");
            result.Errors.Add($"Validation failed with exception: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates a specific tenant's configuration and connections.
    /// </summary>
    public async Task<TenantValidationResult> ValidateTenantAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var result = new TenantValidationResult { TenantId = tenantId };
        _logger.LogInformation("Starting validation for tenant {TenantId}", tenantId);

        try
        {
            // 1. Check if tenant exists
            var (tenant, _) = await _tenantRepository.LoadAsync(tenantId);
            if (tenant == null)
            {
                result.Errors.Add("Tenant not found");
                result.IsValid = false;
                return result;
            }

            result.TenantName = tenant.Name;

            // 2. Validate tenant name format
            if (!IsValidTenantName(tenant.Name))
            {
                result.Errors.Add($"Invalid tenant name format: {tenant.Name}");
            }

            // 3. Test database connection
            try
            {
                var dbOptions = await _connectionProvider.GetDatabaseConnectionAsync(tenantId, cancellationToken);
                result.DatabaseConnectionString = MaskConnectionString(dbOptions.MongoConnectionString);
                
                var dbValid = await TestDatabaseConnectionAsync(dbOptions, cancellationToken);
                result.DatabaseConnectionValid = dbValid;
                
                if (!dbValid)
                {
                    result.Errors.Add("Database connection test failed");
                }
            }
            catch (Exception ex)
            {
                result.DatabaseConnectionValid = false;
                result.Errors.Add($"Failed to get database connection: {ex.Message}");
            }

            // 4. Test storage connection
            try
            {
                var storageOptions = await _connectionProvider.GetStorageConnectionAsync(tenantId, cancellationToken);
                result.StorageConnectionValid = await TestStorageConnectionAsync(storageOptions, cancellationToken);
                
                if (!result.StorageConnectionValid)
                {
                    result.Warnings.Add("Storage connection test failed");
                }
            }
            catch (Exception ex)
            {
                result.StorageConnectionValid = false;
                result.Warnings.Add($"Failed to get storage connection: {ex.Message}");
            }

            // 5. Check tenant status
            if (!tenant.IsActive)
            {
                result.Warnings.Add($"Tenant is deactivated since {tenant.DeactivatedDate}");
            }

            result.IsValid = !result.Errors.Any();
            
            _logger.LogInformation(
                "Tenant validation completed for {TenantId}. Valid: {IsValid}, Warnings: {WarningCount}, Errors: {ErrorCount}",
                tenantId, result.IsValid, result.Warnings.Count, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant validation failed for {TenantId}", tenantId);
            result.Errors.Add($"Validation failed with exception: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates basic configuration settings.
    /// </summary>
    private void ValidateBasicConfiguration(ValidationResult result)
    {
        // Check cache duration
        if (_options.ConnectionCacheDurationMinutes < 1 || _options.ConnectionCacheDurationMinutes > 1440)
        {
            result.Warnings.Add($"Connection cache duration {_options.ConnectionCacheDurationMinutes} minutes may be suboptimal (recommended: 5-60 minutes)");
        }

        // Check global tenant ID
        if (_options.GlobalTenantId == Guid.Empty)
        {
            result.Errors.Add("Global tenant ID cannot be empty");
        }

        // Check tenant claim configuration
        if (string.IsNullOrWhiteSpace(_options.TenantClaimName))
        {
            result.Errors.Add("Tenant claim name is required");
        }

        // Check if header-based tenant switching is enabled (security risk)
        if (_options.AllowTenantSwitchingViaHeaders)
        {
            result.Warnings.Add("Tenant switching via headers is enabled - this may be a security risk in production");
        }
    }

    /// <summary>
    /// Validates connection provider configuration.
    /// </summary>
    private async Task ValidateConnectionProviderAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        var providerType = _options.ConnectionProvider?.ToLowerInvariant();
        
        switch (providerType)
        {
            case "azurekeyvault":
            case "keyvault":
                // Check if Azure Key Vault is properly configured
                if (_connectionProvider is AzureKeyVaultConnectionProvider)
                {
                    try
                    {
                        // Test with global tenant to verify Key Vault access
                        await _connectionProvider.TestConnectionAsync(_options.GlobalTenantId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Azure Key Vault connection provider test failed: {ex.Message}");
                    }
                }
                break;
                
            case "local":
                // Check if encryption key is configured for local provider
                if (string.IsNullOrEmpty(_options.LocalEncryptionKey))
                {
                    result.Warnings.Add("Local encryption key not configured - using auto-generated key");
                }
                break;
        }
    }

    /// <summary>
    /// Validates global tenant configuration.
    /// </summary>
    private async Task ValidateGlobalTenantAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var (globalTenant, _) = await _tenantRepository.LoadAsync(_options.GlobalTenantId);
            if (globalTenant == null)
            {
                if (_options.AutoCreateGlobalTenant)
                {
                    result.Warnings.Add("Global tenant does not exist but will be auto-created");
                }
                else
                {
                    result.Errors.Add("Global tenant does not exist and auto-creation is disabled");
                }
            }
            else
            {
                // Validate global tenant
                var tenantResult = await ValidateTenantAsync(_options.GlobalTenantId, cancellationToken);
                if (!tenantResult.IsValid)
                {
                    result.Errors.Add($"Global tenant validation failed: {string.Join(", ", tenantResult.Errors)}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate global tenant: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates all active tenants.
    /// </summary>
    private async Task ValidateActiveTenantsAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var activeTenants = await _tenantRepository.GetActiveTenantsAsync(cancellationToken);
            result.Info.Add($"Found {activeTenants.Count} active tenants");

            var failedTenants = new List<string>();
            var tasks = activeTenants.Select(async tenant =>
            {
                var tenantResult = await ValidateTenantAsync(tenant.Id, cancellationToken);
                if (!tenantResult.IsValid)
                {
                    lock (failedTenants)
                    {
                        failedTenants.Add($"{tenant.Name} ({tenant.Id}): {string.Join(", ", tenantResult.Errors)}");
                    }
                }
            });

            await Task.WhenAll(tasks);

            if (failedTenants.Any())
            {
                result.Errors.Add($"{failedTenants.Count} tenants failed validation");
                foreach (var failure in failedTenants.Take(10)) // Limit to first 10
                {
                    result.Errors.Add($"  - {failure}");
                }
                
                if (failedTenants.Count > 10)
                {
                    result.Errors.Add($"  ... and {failedTenants.Count - 10} more");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate active tenants: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests a database connection.
    /// </summary>
    private async Task<bool> TestDatabaseConnectionAsync(
        MongoRepositoryOptions options, 
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = MongoClientSettings.FromConnectionString(options.MongoConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            
            var client = new MongoClient(settings);
            var database = client.GetDatabase(options.DatabaseName);
            
            // Ping the database
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), 
                cancellationToken: cancellationToken);
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Tests a storage connection.
    /// </summary>
    private async Task<bool> TestStorageConnectionAsync(
        StorageConnectionOptions options, 
        CancellationToken cancellationToken)
    {
        // This would be implemented based on the storage provider
        // For now, we'll just return true if options are provided
        await Task.CompletedTask;
        return options != null;
    }

    /// <summary>
    /// Validates tenant name format.
    /// </summary>
    private bool IsValidTenantName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
            
        // Tenant name should be lowercase alphanumeric with hyphens
        return Regex.IsMatch(name, @"^[a-z0-9-]+$");
    }

    /// <summary>
    /// Masks sensitive parts of connection string for logging.
    /// </summary>
    private string MaskConnectionString(string connectionString)
    {
        // Mask password and other sensitive parts
        var pattern = @"(password|pwd|apikey|key)=([^;]+)";
        return Regex.Replace(connectionString, pattern, "$1=****", RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// Result of configuration validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Info { get; set; } = new();
}

/// <summary>
/// Result of tenant-specific validation.
/// </summary>
public class TenantValidationResult : ValidationResult
{
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public bool DatabaseConnectionValid { get; set; }
    public bool StorageConnectionValid { get; set; }
    public string? DatabaseConnectionString { get; set; }
}