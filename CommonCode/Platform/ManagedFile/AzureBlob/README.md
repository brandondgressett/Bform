# Azure Blob Storage Managed File Persistence

This directory contains the Azure Blob Storage implementation of the `IManagedFilePersistence` interface.

## Overview

The `AzureBlobFilePersistence` class provides a cloud-based file storage solution using Azure Blob Storage, replacing the local file system implementation with a scalable, distributed storage service.

## Features

- **Container-based Organization**: Maps the folder concept to Azure Blob containers
- **Stream-based Operations**: Maintains compatibility with the existing interface
- **Managed Identity Support**: Secure authentication for production environments
- **Access Tier Management**: Optimize costs with Hot/Cool/Archive tiers
- **Soft Delete**: Recover accidentally deleted files
- **Versioning**: Optional blob versioning for file history
- **Encryption**: Support for encryption scopes
- **Tagging**: Organize and track blobs with metadata tags
- **Retry Logic**: Built-in retry for transient failures

## Configuration

### Connection Options

1. **Connection String** (Development/Testing):
```json
{
  "ManagedFiles": {
    "AzureBlob": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
      "UseManagedIdentity": false
    }
  }
}
```

2. **Managed Identity** (Production):
```json
{
  "ManagedFiles": {
    "AzureBlob": {
      "BlobServiceEndpoint": "https://myaccount.blob.core.windows.net",
      "UseManagedIdentity": true
    }
  }
}
```

### Key Configuration Options

- `BaseContainerName`: Base name for containers (default: "managed-files")
- `MaximumBytes`: Maximum file size limit (default: 100MB)
- `DefaultAccessTier`: Hot, Cool, or Archive (default: Hot)
- `EnableSoftDelete`: Enable blob recovery (default: true)
- `SoftDeleteRetentionDays`: Recovery period (default: 7 days)
- `EnableVersioning`: Track file history (default: false)

## Usage

### Service Registration

```csharp
services.Configure<AzureBlobFilePersistenceOptions>(
    configuration.GetSection("ManagedFiles:AzureBlob"));

services.AddSingleton<IManagedFilePersistence, AzureBlobFilePersistence>();
```

### Example Usage

```csharp
public class MyService
{
    private readonly IManagedFilePersistence _filePersistence;

    public MyService(IManagedFilePersistence filePersistence)
    {
        _filePersistence = filePersistence;
    }

    public async Task SaveFileAsync(string category, string fileId, Stream content)
    {
        await _filePersistence.UpsertAsync(category, fileId, content, CancellationToken.None);
    }

    public async Task<Stream> GetFileAsync(string category, string fileId)
    {
        return await _filePersistence.RetrieveAsync(category, fileId, CancellationToken.None);
    }
}
```

## Container Naming

The implementation automatically creates containers based on the category parameter:
- Base container + category = full container name
- Example: "managed-files-documents" for category "documents"
- Container names are sanitized to meet Azure requirements

## Performance Considerations

1. **Access Tiers**: Use appropriate tiers based on access patterns
   - Hot: Frequently accessed data
   - Cool: Infrequently accessed data (30+ days)
   - Archive: Rarely accessed data (180+ days)

2. **Request Limits**: Azure Storage has request rate limits
   - Consider implementing request throttling for high-volume scenarios

3. **Network Optimization**: Use direct mode and regional endpoints

## Security

1. **Authentication**: Use Managed Identity in production
2. **Encryption**: Data encrypted at rest by default
3. **Network Security**: Use Private Endpoints for network isolation
4. **Access Control**: Implement RBAC for fine-grained permissions

## Cost Optimization

1. **Lifecycle Management**: Automatically move old files to cooler tiers
2. **Reserved Capacity**: Purchase reserved storage for predictable workloads
3. **Monitoring**: Use Azure Monitor to track usage and costs

## Migration from Physical File Storage

1. **Minimal Code Changes**: Same interface implementation
2. **Data Migration**: Use Azure Storage Explorer or AzCopy
3. **Testing**: Thoroughly test in non-production environment first

## Limitations

- Maximum blob size: 5TB (but limited by configuration)
- Container name restrictions: lowercase, 3-63 characters
- Metadata size limit: 8KB per blob

## Troubleshooting

1. **Authentication Errors**: Verify connection string or managed identity setup
2. **404 Errors**: Ensure container exists or enable auto-creation
3. **Throttling**: Implement exponential backoff for retries
4. **Network Issues**: Check firewall rules and network connectivity