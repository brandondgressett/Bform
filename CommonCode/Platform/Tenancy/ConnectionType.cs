namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Defines the types of connections that can be configured per tenant
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Database connection (MongoDB, CosmosDB, etc.)
    /// </summary>
    Database = 0,
    
    /// <summary>
    /// Storage connection (File system, Azure Blob, etc.)
    /// </summary>
    Storage = 1
}