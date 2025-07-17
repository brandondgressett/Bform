namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Configuration options for tenant-specific storage connections.
/// Supports both file system and Azure Blob Storage configurations.
/// </summary>
public class StorageConnectionOptions
{
    /// <summary>
    /// Storage provider type (e.g., "FileSystem", "AzureBlob")
    /// </summary>
    public string Provider { get; set; } = "FileSystem";
    
    /// <summary>
    /// Connection string for storage (Azure connection string or file path)
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Base path or container name for storage
    /// </summary>
    public string? BasePath { get; set; }
    
    /// <summary>
    /// Whether to use managed identity for authentication (Azure only)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;
    
    /// <summary>
    /// Service endpoint URI for managed identity scenarios (Azure only)
    /// </summary>
    public string? ServiceEndpoint { get; set; }
    
    /// <summary>
    /// Default access tier for content (Azure only: Hot, Cool, Archive)
    /// </summary>
    public string DefaultAccessTier { get; set; } = "Cool";
    
    /// <summary>
    /// Enable versioning for stored content
    /// </summary>
    public bool EnableVersioning { get; set; } = true;
    
    /// <summary>
    /// Maximum number of versions to retain
    /// </summary>
    public int MaxVersionCount { get; set; } = 10;
    
    /// <summary>
    /// Enable soft delete for content recovery
    /// </summary>
    public bool EnableSoftDelete { get; set; } = true;
    
    /// <summary>
    /// Soft delete retention period in days
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Request timeout in seconds for storage operations
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;
    
    /// <summary>
    /// Maximum number of retry attempts for transient failures
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Enable caching of storage content for better performance
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Additional provider-specific settings
    /// </summary>
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}