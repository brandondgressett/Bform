namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Configuration options for multi-tenancy features.
/// Controls whether multi-tenancy is enabled and how tenant resolution works.
/// </summary>
public class MultiTenancyOptions
{
    /// <summary>
    /// Configuration section name for binding
    /// </summary>
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Whether multi-tenancy is enabled. When false, system operates in single-tenant mode.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Default tenant ID to use when multi-tenancy is disabled or no tenant is specified.
    /// If not specified, a global tenant will be automatically created.
    /// </summary>
    public Guid? DefaultTenantId { get; set; }

    /// <summary>
    /// Global tenant ID used for system-wide operations and single-tenant mode.
    /// This tenant is automatically created when multi-tenancy is disabled.
    /// </summary>
    public Guid GlobalTenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Connection provider type to use (Local, Cached, etc.)
    /// </summary>
    public string ConnectionProvider { get; set; } = "Cached";

    /// <summary>
    /// Whether to automatically create the global tenant if it doesn't exist
    /// </summary>
    public bool AutoCreateGlobalTenant { get; set; } = true;

    /// <summary>
    /// Global tenant name
    /// </summary>
    public string GlobalTenantName { get; set; } = "global";

    /// <summary>
    /// Global tenant display name
    /// </summary>
    public string GlobalTenantDisplayName { get; set; } = "Global Tenant";

    /// <summary>
    /// Whether to require explicit tenant specification in multi-tenant mode
    /// </summary>
    public bool RequireExplicitTenant { get; set; } = true;

    /// <summary>
    /// JWT claim name containing the tenant identifier
    /// </summary>
    public string TenantClaimName { get; set; } = "tenant_id";

    /// <summary>
    /// Alternative JWT claim name for tenant name/code
    /// </summary>
    public string TenantNameClaimName { get; set; } = "tenant_name";

    /// <summary>
    /// HTTP header name for tenant specification (fallback if not in JWT)
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Whether to allow tenant switching via HTTP headers (security consideration)
    /// </summary>
    public bool AllowTenantSwitchingViaHeaders { get; set; } = false;

    /// <summary>
    /// Connection cache duration in minutes
    /// </summary>
    public int ConnectionCacheDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to validate tenant existence during context resolution
    /// </summary>
    public bool ValidateTenantExistence { get; set; } = true;

    /// <summary>
    /// Additional configuration settings for extensibility
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();

    /// <summary>
    /// Encryption key for local connection string storage (base64 encoded)
    /// </summary>
    public string? LocalEncryptionKey { get; set; }
}