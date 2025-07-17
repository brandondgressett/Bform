using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.ManagedFiles.AzureBlob;

/// <summary>
/// Azure Blob Storage implementation of IManagedFilePersistence interface.
/// Stores and retrieves file contents using Azure Blob Storage service.
/// </summary>
public class AzureBlobFilePersistence : IManagedFilePersistence
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobFilePersistenceOptions _options;
    private readonly IApplicationAlert _alerts;
    private readonly ILogger<AzureBlobFilePersistence> _logger;

    public AzureBlobFilePersistence(
        IOptions<AzureBlobFilePersistenceOptions> options,
        IApplicationAlert alerts,
        ILogger<AzureBlobFilePersistence> logger)
    {
        _options = options.Value;
        _alerts = alerts;
        _logger = logger;

        // Initialize BlobServiceClient based on authentication method
        if (_options.UseManagedIdentity)
        {
            if (string.IsNullOrEmpty(_options.BlobServiceEndpoint))
            {
                throw new InvalidOperationException("BlobServiceEndpoint must be specified when using Managed Identity");
            }

            var credential = new DefaultAzureCredential();
            _blobServiceClient = new BlobServiceClient(new Uri(_options.BlobServiceEndpoint), credential);
            _logger.LogInformation("Initialized Azure Blob Storage with Managed Identity");
        }
        else
        {
            if (string.IsNullOrEmpty(_options.ConnectionString))
            {
                throw new InvalidOperationException("ConnectionString must be specified when not using Managed Identity");
            }

            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _logger.LogInformation("Initialized Azure Blob Storage with connection string");
        }
    }

    /// <summary>
    /// Retrieves file contents from Azure Blob Storage.
    /// </summary>
    /// <param name="container">The container name (will be prefixed with base container)</param>
    /// <param name="id">The file ID (used as blob name)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Stream containing the file data</returns>
    public async Task<Stream> RetrieveAsync(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/')); 
            id.Requires().IsNotNullOrEmpty();

            var containerName = GetContainerName(container);
            var blobName = GetBlobName(id);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            ct.ThrowIfCancellationRequested();

            // Check if blob exists
            var response = await blobClient.ExistsAsync(ct);
            if (!response.Value)
            {
                throw new FileNotFoundException($"Blob '{id}' not found in container '{container}'");
            }

            // Download blob to stream
            var downloadResponse = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
            
            _logger.LogDebug("Retrieved blob {BlobName} from container {ContainerName}", blobName, containerName);
            
            return downloadResponse.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"Blob '{id}' not found in container '{container}'", ex);
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Services,
                LogLevel.Error,
                $"Failed to retrieve blob: {ex.Message}",
                _options.ErrorThreshold,
                nameof(AzureBlobFilePersistence));
            throw;
        }
    }

    /// <summary>
    /// Creates or updates file contents in Azure Blob Storage.
    /// </summary>
    /// <param name="container">The container name (will be prefixed with base container)</param>
    /// <param name="id">The file ID (used as blob name)</param>
    /// <param name="stream">The file data stream</param>
    /// <param name="ct">Cancellation token</param>
    public async Task UpsertAsync(string container, string id, Stream stream, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/'));
            id.Requires().IsNotNullOrEmpty();
            stream.Requires().IsNotNull();
            
            // Check file size
            if (stream.Length > _options.MaximumBytes)
            {
                throw new InvalidOperationException($"File size {stream.Length} exceeds maximum allowed size {_options.MaximumBytes}");
            }

            var containerName = GetContainerName(container);
            var blobName = GetBlobName(id);

            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var blobClient = containerClient.GetBlobClient(blobName);

            // Set blob options
            var blobUploadOptions = new BlobUploadOptions
            {
                AccessTier = Enum.Parse<AccessTier>(_options.DefaultAccessTier, true),
                Metadata = new Dictionary<string, string>
                {
                    { "container", container },
                    { "id", id },
                    { "uploaded", DateTimeOffset.UtcNow.ToString("O") }
                }
            };

            // Add tags if configured
            if (_options.DefaultBlobTags.Any())
            {
                blobUploadOptions.Tags = new Dictionary<string, string>(_options.DefaultBlobTags);
            }

            // Upload blob
            await blobClient.UploadAsync(stream, blobUploadOptions, ct);

            _logger.LogDebug("Uploaded blob {BlobName} to container {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Services,
                LogLevel.Error,
                $"Failed to upload blob: {ex.Message}",
                _options.ErrorThreshold,
                nameof(AzureBlobFilePersistence));
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from Azure Blob Storage.
    /// </summary>
    /// <param name="container">The container name (will be prefixed with base container)</param>
    /// <param name="id">The file ID (used as blob name)</param>
    /// <param name="ct">Cancellation token</param>
    public async Task DeleteAsync(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/'));
            id.Requires().IsNotNullOrEmpty();

            var containerName = GetContainerName(container);
            var blobName = GetBlobName(id);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            ct.ThrowIfCancellationRequested();

            // Delete blob (soft delete if enabled)
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);

            _logger.LogDebug("Deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Services,
                LogLevel.Error,
                $"Failed to delete blob: {ex.Message}",
                _options.ErrorThreshold,
                nameof(AzureBlobFilePersistence));
            throw;
        }
    }

    /// <summary>
    /// Checks if a file exists in Azure Blob Storage.
    /// </summary>
    /// <param name="container">The container name (will be prefixed with base container)</param>
    /// <param name="id">The file ID (used as blob name)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the blob exists, false otherwise</returns>
    public async Task<bool> Exists(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/'));
            id.Requires().IsNotNullOrEmpty();

            var containerName = GetContainerName(container);
            var blobName = GetBlobName(id);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            ct.ThrowIfCancellationRequested();

            var response = await blobClient.ExistsAsync(ct);
            return response.Value;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Services,
                LogLevel.Error,
                $"Failed to check blob existence: {ex.Message}",
                _options.ErrorThreshold,
                nameof(AzureBlobFilePersistence));
            throw;
        }
    }

    /// <summary>
    /// Gets the full container name by combining base container with the specified container.
    /// </summary>
    private string GetContainerName(string container)
    {
        // Azure container names must be lowercase and 3-63 characters
        var fullName = $"{_options.BaseContainerName}-{container}".ToLowerInvariant();
        
        // Ensure container name is valid
        if (fullName.Length > 63)
        {
            fullName = fullName.Substring(0, 63);
        }
        
        // Replace any invalid characters with hyphens
        fullName = System.Text.RegularExpressions.Regex.Replace(fullName, @"[^a-z0-9-]", "-");
        
        // Remove consecutive hyphens
        fullName = System.Text.RegularExpressions.Regex.Replace(fullName, @"-+", "-");
        
        // Trim hyphens from start and end
        fullName = fullName.Trim('-');
        
        return fullName;
    }

    /// <summary>
    /// Gets the blob name from the file ID.
    /// </summary>
    private string GetBlobName(string id)
    {
        // Add .bin extension if not present
        return id.EndsWith(".bin") ? id : $"{id}.bin";
    }
}