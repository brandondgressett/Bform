using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Health check for multi-tenant system components.
/// Monitors tenant repository access, connection providers, and overall system health.
/// </summary>
public class TenantHealthCheck : IHealthCheck
{
    private readonly TenantRepository _tenantRepository;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantHealthCheck> _logger;

    public TenantHealthCheck(
        TenantRepository tenantRepository,
        ITenantConnectionProvider connectionProvider,
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantHealthCheck> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        var unhealthyReasons = new List<string>();

        try
        {
            // Check if multi-tenancy is enabled
            data["MultiTenancyEnabled"] = _options.Enabled;

            if (!_options.Enabled)
            {
                // Single-tenant mode - just verify basic connectivity
                data["Mode"] = "SingleTenant";
                return HealthCheckResult.Healthy("Multi-tenancy is disabled (single-tenant mode)", data);
            }

            // Multi-tenant mode health checks
            data["Mode"] = "MultiTenant";
            
            // 1. Check tenant repository access
            var tenantRepoHealthy = await CheckTenantRepositoryAsync(data, cancellationToken);
            if (!tenantRepoHealthy)
            {
                unhealthyReasons.Add("Tenant repository is not accessible");
            }

            // 2. Check current tenant context
            var tenantContextHealthy = CheckTenantContext(data);
            if (!tenantContextHealthy)
            {
                unhealthyReasons.Add("Tenant context is not properly configured");
            }

            // 3. Check connection provider
            var connectionProviderHealthy = await CheckConnectionProviderAsync(data, cancellationToken);
            if (!connectionProviderHealthy)
            {
                unhealthyReasons.Add("Connection provider is not functioning properly");
            }

            // 4. Check specific tenant health if in tenant context
            if (_tenantContext.CurrentTenantId.HasValue)
            {
                var tenantHealthy = await CheckSpecificTenantHealthAsync(
                    _tenantContext.CurrentTenantId.Value, 
                    data, 
                    cancellationToken);
                if (!tenantHealthy)
                {
                    unhealthyReasons.Add($"Current tenant {_tenantContext.CurrentTenantId} has health issues");
                }
            }

            stopwatch.Stop();
            data["ElapsedMilliseconds"] = stopwatch.ElapsedMilliseconds;

            // Determine overall health status
            if (unhealthyReasons.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Multi-tenancy system has issues: {string.Join("; ", unhealthyReasons)}",
                    null,
                    data);
            }

            return HealthCheckResult.Healthy(
                "Multi-tenancy system is healthy",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            stopwatch.Stop();
            data["ElapsedMilliseconds"] = stopwatch.ElapsedMilliseconds;
            data["Exception"] = ex.GetType().Name;
            data["ExceptionMessage"] = ex.Message;

            return HealthCheckResult.Unhealthy(
                "Multi-tenancy health check failed with exception",
                ex,
                data);
        }
    }

    private async Task<bool> CheckTenantRepositoryAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var tenants = await _tenantRepository.GetActiveTenantsAsync(cancellationToken);
            stopwatch.Stop();

            data["TenantRepositoryAccessible"] = true;
            data["ActiveTenantCount"] = tenants.Count;
            data["TenantRepositoryResponseTime"] = stopwatch.ElapsedMilliseconds;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to access tenant repository");
            data["TenantRepositoryAccessible"] = false;
            data["TenantRepositoryError"] = ex.Message;
            return false;
        }
    }

    private bool CheckTenantContext(Dictionary<string, object> data)
    {
        try
        {
            data["CurrentTenantId"] = _tenantContext.CurrentTenantId?.ToString() ?? "None";
            data["IsRootUser"] = _tenantContext.IsRootUser;
            data["CurrentUser"] = _tenantContext.CurrentUser?.UserName ?? "Anonymous";

            // Check if tenant context is properly initialized
            if (_options.Enabled && !_tenantContext.IsRootUser && !_tenantContext.CurrentTenantId.HasValue)
            {
                data["TenantContextHealthy"] = false;
                data["TenantContextIssue"] = "No tenant context in multi-tenant mode";
                return false;
            }

            data["TenantContextHealthy"] = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check tenant context");
            data["TenantContextHealthy"] = false;
            data["TenantContextError"] = ex.Message;
            return false;
        }
    }

    private async Task<bool> CheckConnectionProviderAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check connection provider type
            data["ConnectionProviderType"] = _connectionProvider.GetType().Name;

            // If we have a current tenant, test its connections
            if (_tenantContext.CurrentTenantId.HasValue)
            {
                var stopwatch = Stopwatch.StartNew();
                var connectionTests = await _connectionProvider.TestConnectionAsync(
                    _tenantContext.CurrentTenantId.Value,
                    cancellationToken);
                stopwatch.Stop();

                data["ConnectionTestResponseTime"] = stopwatch.ElapsedMilliseconds;
                data["DatabaseConnectionHealthy"] = connectionTests.GetValueOrDefault(ConnectionType.Database);
                data["StorageConnectionHealthy"] = connectionTests.GetValueOrDefault(ConnectionType.Storage);

                return connectionTests.All(ct => ct.Value);
            }

            data["ConnectionProviderHealthy"] = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check connection provider");
            data["ConnectionProviderHealthy"] = false;
            data["ConnectionProviderError"] = ex.Message;
            return false;
        }
    }

    private async Task<bool> CheckSpecificTenantHealthAsync(
        Guid tenantId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantData = new Dictionary<string, object>();
            
            // Load tenant information
            var (tenant, _) = await _tenantRepository.LoadAsync(tenantId);
            if (tenant == null)
            {
                tenantData["Exists"] = false;
                data[$"Tenant_{tenantId}"] = tenantData;
                return false;
            }

            tenantData["Exists"] = true;
            tenantData["Name"] = tenant.Name;
            tenantData["IsActive"] = tenant.IsActive;
            tenantData["CreatedDate"] = tenant.CreatedDate;

            if (!tenant.IsActive)
            {
                tenantData["Healthy"] = false;
                tenantData["Reason"] = "Tenant is deactivated";
                data[$"Tenant_{tenantId}"] = tenantData;
                return false;
            }

            // Test connections
            var connectionTests = await _connectionProvider.TestConnectionAsync(tenantId, cancellationToken);
            tenantData["DatabaseConnection"] = connectionTests.GetValueOrDefault(ConnectionType.Database);
            tenantData["StorageConnection"] = connectionTests.GetValueOrDefault(ConnectionType.Storage);

            var isHealthy = connectionTests.All(ct => ct.Value);
            tenantData["Healthy"] = isHealthy;
            
            if (!isHealthy)
            {
                tenantData["Reason"] = "One or more connections are unhealthy";
            }

            data[$"Tenant_{tenantId}"] = tenantData;
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check specific tenant health for {TenantId}", tenantId);
            data[$"Tenant_{tenantId}_Error"] = ex.Message;
            return false;
        }
    }
}

/// <summary>
/// Health check for monitoring all tenants in the system.
/// This is a more comprehensive check that validates all active tenants.
/// </summary>
public class AllTenantsHealthCheck : IHealthCheck
{
    private readonly TenantRepository _tenantRepository;
    private readonly ITenantConnectionProvider _connectionProvider;
    private readonly ILogger<AllTenantsHealthCheck> _logger;
    private readonly int _maxTenantsToCheck;
    private readonly int _unhealthyThreshold;

    public AllTenantsHealthCheck(
        TenantRepository tenantRepository,
        ITenantConnectionProvider connectionProvider,
        ILogger<AllTenantsHealthCheck> logger,
        int maxTenantsToCheck = 100,
        int unhealthyThreshold = 10)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxTenantsToCheck = maxTenantsToCheck;
        _unhealthyThreshold = unhealthyThreshold;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        var unhealthyTenants = new List<string>();

        try
        {
            // Get all active tenants
            var activeTenants = await _tenantRepository.GetActiveTenantsAsync(cancellationToken);
            data["TotalActiveTenants"] = activeTenants.Count;

            // Limit the number of tenants to check for performance
            var tenantsToCheck = activeTenants.Take(_maxTenantsToCheck).ToList();
            data["TenantsChecked"] = tenantsToCheck.Count;

            // Check each tenant in parallel
            var tasks = tenantsToCheck.Select(async tenant =>
            {
                try
                {
                    var connectionTests = await _connectionProvider.TestConnectionAsync(
                        tenant.Id,
                        cancellationToken);
                    
                    var isHealthy = connectionTests.All(ct => ct.Value);
                    if (!isHealthy)
                    {
                        lock (unhealthyTenants)
                        {
                            unhealthyTenants.Add($"{tenant.Name} ({tenant.Id})");
                        }
                    }
                    
                    return isHealthy;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check tenant {TenantId} health", tenant.Id);
                    lock (unhealthyTenants)
                    {
                        unhealthyTenants.Add($"{tenant.Name} ({tenant.Id}) - Error: {ex.Message}");
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            var healthyCount = results.Count(r => r);
            var unhealthyCount = results.Count(r => !r);

            stopwatch.Stop();
            data["ElapsedMilliseconds"] = stopwatch.ElapsedMilliseconds;
            data["HealthyTenants"] = healthyCount;
            data["UnhealthyTenants"] = unhealthyCount;
            
            if (unhealthyTenants.Any())
            {
                data["UnhealthyTenantList"] = unhealthyTenants.Take(10).ToList(); // Limit details for readability
            }

            // Determine health status based on threshold
            if (unhealthyCount == 0)
            {
                return HealthCheckResult.Healthy(
                    $"All {tenantsToCheck.Count} checked tenants are healthy",
                    data);
            }
            else if (unhealthyCount <= _unhealthyThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"{unhealthyCount} out of {tenantsToCheck.Count} tenants are unhealthy",
                    null,
                    data);
            }
            else
            {
                return HealthCheckResult.Unhealthy(
                    $"{unhealthyCount} out of {tenantsToCheck.Count} tenants are unhealthy (exceeds threshold of {_unhealthyThreshold})",
                    null,
                    data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All tenants health check failed with exception");
            stopwatch.Stop();
            data["ElapsedMilliseconds"] = stopwatch.ElapsedMilliseconds;
            data["Exception"] = ex.GetType().Name;
            data["ExceptionMessage"] = ex.Message;

            return HealthCheckResult.Unhealthy(
                "All tenants health check failed with exception",
                ex,
                data);
        }
    }
}

/// <summary>
/// Extension methods for registering tenant health checks.
/// </summary>
public static class TenantHealthCheckExtensions
{
    /// <summary>
    /// Adds tenant health checks to the health check builder.
    /// </summary>
    public static IHealthChecksBuilder AddTenantHealthChecks(
        this IHealthChecksBuilder builder,
        string? tenantHealthCheckName = "tenant_system",
        string? allTenantsHealthCheckName = "all_tenants",
        HealthStatus? tenantFailureStatus = HealthStatus.Unhealthy,
        HealthStatus? allTenantsFailureStatus = HealthStatus.Degraded,
        IEnumerable<string>? tenantTags = null,
        IEnumerable<string>? allTenantsTags = null,
        TimeSpan? tenantTimeout = null,
        TimeSpan? allTenantsTimeout = null)
    {
        // Add the basic tenant system health check
        if (!string.IsNullOrEmpty(tenantHealthCheckName))
        {
            builder.AddTypeActivatedCheck<TenantHealthCheck>(
                tenantHealthCheckName,
                failureStatus: tenantFailureStatus,
                tags: tenantTags?.ToArray() ?? new[] { "tenant", "system" });
        }

        // Add the all tenants health check
        if (!string.IsNullOrEmpty(allTenantsHealthCheckName))
        {
            builder.AddTypeActivatedCheck<AllTenantsHealthCheck>(
                allTenantsHealthCheckName,
                failureStatus: allTenantsFailureStatus,
                tags: allTenantsTags?.ToArray() ?? new[] { "tenant", "monitoring" },
                timeout: allTenantsTimeout ?? TimeSpan.FromSeconds(30));
        }

        return builder;
    }
}