namespace BFormDomain.CommonCode.Platform.ManagedFiles.AzureBlob;

/// <summary>
/// Configuration options for Azure Blob Storage file persistence.
/// </summary>
public class AzureBlobFilePersistenceOptions
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
    /// The base container name. If not specified, defaults to "managed-files".
    /// Container names must be lowercase, 3-63 characters, start with letter/number.
    /// </summary>
    public string BaseContainerName { get; set; } = "managed-files";

    /// <summary>
    /// Whether to use managed identity for authentication.
    /// If true, BlobServiceEndpoint must be specified.
    /// If false, ConnectionString must be specified.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Maximum file size in bytes. Default is 100MB.
    /// Azure Blob Storage supports up to 5TB per blob, but we limit for cost/performance.
    /// </summary>
    public long MaximumBytes { get; set; } = 1024 * 1024 * 100;

    /// <summary>
    /// Number of exceptions before raising an operational alert.
    /// </summary>
    public int ErrorThreshold { get; set; } = 15;

    /// <summary>
    /// Default blob access tier for new blobs.
    /// Options: Hot, Cool, Archive. Default is Hot for frequently accessed data.
    /// </summary>
    public string DefaultAccessTier { get; set; } = "Hot";

    /// <summary>
    /// Enable soft delete for blobs. Allows recovery of deleted blobs.
    /// </summary>
    public bool EnableSoftDelete { get; set; } = true;

    /// <summary>
    /// Soft delete retention period in days. Default is 7 days.
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 7;

    /// <summary>
    /// Enable blob versioning for automatic version history.
    /// </summary>
    public bool EnableVersioning { get; set; } = false;

    /// <summary>
    /// Enable encryption scopes for additional security isolation.
    /// </summary>
    public bool EnableEncryptionScope { get; set; } = false;

    /// <summary>
    /// The encryption scope name if EnableEncryptionScope is true.
    /// </summary>
    public string? EncryptionScopeName { get; set; }

    /// <summary>
    /// Request timeout in seconds for blob operations.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Enable diagnostic logging for blob operations.
    /// </summary>
    public bool EnableDiagnosticLogging { get; set; } = true;

    /// <summary>
    /// Tags to apply to all blobs for cost tracking and organization.
    /// </summary>
    public Dictionary<string, string> DefaultBlobTags { get; set; } = new();
}