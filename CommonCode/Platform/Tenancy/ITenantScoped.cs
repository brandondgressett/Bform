namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Interface for entities that are scoped to a specific tenant.
/// Implementing classes will have their data automatically filtered by tenant context.
/// </summary>
public interface ITenantScoped
{
    /// <summary>
    /// The unique identifier of the tenant that owns this entity.
    /// </summary>
    Guid TenantId { get; set; }
}