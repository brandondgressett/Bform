using BFormDomain.DataModels;
using MongoDbGenericRepository.Attributes;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Represents a connection configuration for a specific tenant.
/// Stores encrypted connection details for database and storage access.
/// Note: This entity is NOT tenant-scoped as it manages the tenant connections themselves.
/// </summary>
[CollectionName("TenantConnections")]
public class TenantConnection : IDataModel
{
    /// <summary>
    /// Unique identifier for this connection configuration
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Version for optimistic concurrency control
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The tenant this connection belongs to
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Type of connection (Database, Storage)
    /// </summary>
    public ConnectionType Type { get; set; }
    
    /// <summary>
    /// Provider type (e.g., "MongoDB", "AzureBlob", "FileSystem")
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Encrypted connection string for security
    /// </summary>
    public string EncryptedConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Database name (for database connections)
    /// </summary>
    public string? DatabaseName { get; set; }
    
    /// <summary>
    /// Container prefix for storage isolation (for storage connections)
    /// </summary>
    public string? ContainerPrefix { get; set; }
    
    /// <summary>
    /// Additional provider-specific settings
    /// </summary>
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}