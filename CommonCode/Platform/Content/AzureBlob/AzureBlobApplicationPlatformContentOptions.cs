namespace BFormDomain.CommonCode.Platform.Content.AzureBlob;

/// <summary>
/// Configuration options for Azure Blob Storage-based application platform content.
/// </summary>
public class AzureBlobApplicationPlatformContentOptions
{
    /// <summary>
    /// Azure Storage connection string or the URI with SAS token.
    /// For production, use Managed Identity with BlobServiceEndpoint instead.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The blob service endpoint URI (e.g., https://myaccount.blob.core.windows.net).
    /// Use this with Managed Identity for production scenarios.
    /// </summary>
    public string? BlobServiceEndpoint { get; set; }

    /// <summary>
    /// Whether to use managed identity for authentication.
    /// If true, BlobServiceEndpoint must be specified.
    /// If false, ConnectionString must be specified.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Container name for storing content schemas.
    /// </summary>
    public string SchemaContainerName { get; set; } = "content-schemas";

    /// <summary>
    /// Container name for storing content instances.
    /// </summary>
    public string ContentContainerName { get; set; } = "content-instances";

    /// <summary>
    /// Container name for storing free JSON content.
    /// </summary>
    public string FreeJsonContainerName { get; set; } = "content-freejson";

    /// <summary>
    /// Enable caching of content for better performance.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Enable CDN integration for content delivery.
    /// </summary>
    public bool EnableCdn { get; set; } = false;

    /// <summary>
    /// CDN endpoint URL if EnableCdn is true.
    /// </summary>
    public string? CdnEndpointUrl { get; set; }

    /// <summary>
    /// Default access tier for content blobs.
    /// Options: Hot, Cool, Archive. Default is Cool for infrequently accessed content.
    /// </summary>
    public string DefaultAccessTier { get; set; } = "Cool";

    /// <summary>
    /// Enable blob versioning for content history.
    /// </summary>
    public bool EnableVersioning { get; set; } = true;

    /// <summary>
    /// Maximum number of versions to keep.
    /// </summary>
    public int MaxVersionCount { get; set; } = 10;

    /// <summary>
    /// Enable soft delete for content recovery.
    /// </summary>
    public bool EnableSoftDelete { get; set; } = true;

    /// <summary>
    /// Soft delete retention period in days.
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>
    /// Tags to apply to all content blobs for organization.
    /// </summary>
    public Dictionary<string, string> DefaultBlobTags { get; set; } = new()
    {
        { "System", "BFormDomain" },
        { "Component", "Content" }
    };

    /// <summary>
    /// Enable automatic initialization from blob storage on startup.
    /// </summary>
    public bool AutoInitialize { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds for blob operations.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable diagnostic logging for blob operations.
    /// </summary>
    public bool EnableDiagnosticLogging { get; set; } = true;
}