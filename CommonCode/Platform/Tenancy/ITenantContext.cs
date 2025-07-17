using BFormDomain.CommonCode.Authorization;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Provides access to the current tenant context within a request or operation.
/// This service is scoped to individual requests and maintains tenant state.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant identifier for this context.
    /// Returns null if no tenant is set or in single-tenant mode.
    /// </summary>
    Guid? CurrentTenantId { get; }
    
    /// <summary>
    /// The current tenant identifier as a string for compatibility with entity TenantId fields.
    /// Returns null if no tenant is set or in single-tenant mode.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The current user associated with this context.
    /// May be null for unauthenticated requests.
    /// </summary>
    ApplicationUser? CurrentUser { get; }

    /// <summary>
    /// Whether the current user is a root/system user with cross-tenant access.
    /// Root users can access data across multiple tenants.
    /// </summary>
    bool IsRootUser { get; }

    /// <summary>
    /// Whether multi-tenancy is enabled in the current configuration.
    /// </summary>
    bool IsMultiTenancyEnabled { get; }

    /// <summary>
    /// Checks if the current user has access to the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to check access for</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    bool HasAccessToTenant(Guid tenantId);

    /// <summary>
    /// Sets the current tenant context (used by middleware).
    /// </summary>
    /// <param name="tenantId">The tenant identifier to set</param>
    void SetCurrentTenant(Guid? tenantId);

    /// <summary>
    /// Sets the current user context (used by middleware).
    /// </summary>
    /// <param name="user">The user to set</param>
    void SetCurrentUser(ApplicationUser? user);
}