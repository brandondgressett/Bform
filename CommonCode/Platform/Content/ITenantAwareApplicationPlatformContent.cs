using BFormDomain.CommonCode.Platform.Tenancy;
using Newtonsoft.Json.Schema;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Tenant-aware content repository interface that provides access to tenant-specific content.
/// Each tenant has its own isolated set of content (rules, forms, workflows, etc.).
/// </summary>
public interface ITenantAwareApplicationPlatformContent : IApplicationPlatformContent
{
    /// <summary>
    /// Gets the tenant ID this content repository is serving.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Reloads content from the source for this tenant.
    /// Useful for dynamic content updates.
    /// </summary>
    Task ReloadContentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last time content was loaded/refreshed for this tenant.
    /// </summary>
    DateTime LastRefreshed { get; }
}