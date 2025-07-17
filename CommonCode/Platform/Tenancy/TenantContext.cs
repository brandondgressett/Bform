using BFormDomain.CommonCode.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Implementation of ITenantContext that manages tenant state for the current request.
/// Extracts tenant information from JWT claims and HTTP context.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly MultiTenancyOptions _options;
    private readonly TenantRepository _tenantRepository;
    private readonly ILogger<TenantContext> _logger;
    
    private Guid? _currentTenantId;
    private ApplicationUser? _currentUser;
    private bool _tenantValidated = false;

    public TenantContext(
        IOptions<MultiTenancyOptions> options,
        TenantRepository tenantRepository,
        ILogger<TenantContext> logger)
    {
        _options = options.Value;
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public Guid? CurrentTenantId => _currentTenantId;
    
    public string? TenantId => _currentTenantId?.ToString();

    public ApplicationUser? CurrentUser => _currentUser;

    public bool IsRootUser => _currentUser?.Tags?.Contains("root") == true || _currentUser?.Tags?.Contains("admin") == true;

    public bool IsMultiTenancyEnabled => _options.Enabled;

    public bool HasAccessToTenant(Guid tenantId)
    {
        // Root users have access to all tenants
        if (IsRootUser)
        {
            return true;
        }

        // If multi-tenancy is disabled, all users have access to the global tenant
        if (!_options.Enabled)
        {
            return tenantId == _options.GlobalTenantId;
        }

        // Users have access to their current tenant
        return _currentTenantId == tenantId;
    }

    public void SetCurrentTenant(Guid? tenantId)
    {
        if (tenantId == _currentTenantId)
        {
            return; // No change needed
        }

        _currentTenantId = tenantId;
        _tenantValidated = false; // Reset validation flag when tenant changes

        _logger.LogDebug("Set current tenant to {TenantId}", tenantId);

        // Validate tenant exists if validation is enabled
        if (_options.ValidateTenantExistence && tenantId.HasValue && tenantId != _options.GlobalTenantId)
        {
            _ = ValidateTenantExistsAsync(tenantId.Value);
        }
    }

    public void SetCurrentUser(ApplicationUser? user)
    {
        _currentUser = user;
        _logger.LogDebug("Set current user to {UserId}", user?.Id);

        // Extract tenant information from user claims if available
        if (user != null && !_currentTenantId.HasValue)
        {
            ExtractTenantFromUserClaims(user);
        }
    }

    /// <summary>
    /// Extracts tenant information from JWT claims in the user object
    /// </summary>
    private void ExtractTenantFromUserClaims(ApplicationUser user)
    {
        if (user.Claims == null)
        {
            return;
        }

        // Try to get tenant ID from claims
        var tenantIdClaim = user.Claims.FirstOrDefault(c => c.Type == _options.TenantClaimName);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            SetCurrentTenant(tenantId);
            _logger.LogDebug("Extracted tenant {TenantId} from user claims", tenantId);
            return;
        }

        // Try to get tenant by name from claims
        var tenantNameClaim = user.Claims.FirstOrDefault(c => c.Type == _options.TenantNameClaimName);
        if (tenantNameClaim != null)
        {
            _ = ResolveTenantByNameAsync(tenantNameClaim.Value);
        }
    }

    /// <summary>
    /// Resolves tenant ID by tenant name asynchronously
    /// </summary>
    private async Task ResolveTenantByNameAsync(string tenantName)
    {
        try
        {
            var tenant = await _tenantRepository.GetByNameAsync(tenantName);
            if (tenant != null)
            {
                SetCurrentTenant(tenant.Id);
                _logger.LogDebug("Resolved tenant {TenantName} to {TenantId}", tenantName, tenant.Id);
            }
            else
            {
                _logger.LogWarning("Tenant not found by name: {TenantName}", tenantName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant by name: {TenantName}", tenantName);
        }
    }

    /// <summary>
    /// Validates that a tenant exists asynchronously
    /// </summary>
    private async Task ValidateTenantExistsAsync(Guid tenantId)
    {
        if (_tenantValidated)
        {
            return;
        }

        try
        {
            var (tenant, _) = await _tenantRepository.LoadAsync(tenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} does not exist", tenantId);
                // Reset to global tenant or null
                SetCurrentTenant(_options.Enabled ? null : _options.GlobalTenantId);
            }
            else if (!tenant.IsActive)
            {
                _logger.LogWarning("Tenant {TenantId} is not active", tenantId);
                // Reset to global tenant or null
                SetCurrentTenant(_options.Enabled ? null : _options.GlobalTenantId);
            }
            else
            {
                _tenantValidated = true;
                _logger.LogDebug("Validated tenant {TenantId} exists and is active", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant existence: {TenantId}", tenantId);
        }
    }
}