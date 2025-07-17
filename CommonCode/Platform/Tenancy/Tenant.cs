using BFormDomain.DataModels;
using MongoDbGenericRepository.Attributes;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Represents a tenant in the multi-tenant system.
/// Tenants are not tenant-scoped themselves as they represent the global tenant registry.
/// </summary>
[CollectionName("Tenants")]
public class Tenant : IDataModel
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Version for optimistic concurrency control
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Unique name/identifier for the tenant (used in URLs, connection lookups)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable display name for the tenant
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this tenant is active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When this tenant was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this tenant was last updated
    /// </summary>
    public DateTime? UpdatedDate { get; set; }
    
    /// <summary>
    /// When this tenant was deactivated (if applicable)
    /// </summary>
    public DateTime? DeactivatedDate { get; set; }
    
    /// <summary>
    /// Reason for deactivation (if deactivated)
    /// </summary>
    public string? DeactivationReason { get; set; }
    
    /// <summary>
    /// When this tenant was reactivated after deactivation (if applicable)
    /// </summary>
    public DateTime? ReactivatedDate { get; set; }
    
    /// <summary>
    /// The content template set used to initialize this tenant's data
    /// </summary>
    public Guid ContentTemplateSetId { get; set; }
    
    /// <summary>
    /// Arbitrary settings dictionary for tenant-specific configuration
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();
    
    /// <summary>
    /// Tags for grouping and categorizing tenants
    /// </summary>
    public List<string> Tags { get; set; } = new();
}