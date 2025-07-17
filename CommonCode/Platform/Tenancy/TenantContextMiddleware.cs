using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Middleware that establishes tenant context for each HTTP request.
/// Extracts tenant information from JWT claims or HTTP headers and sets up the tenant context.
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(
        RequestDelegate next,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, UserRepository userRepository)
    {
        try
        {
            await SetupTenantContextAsync(context, tenantContext, userRepository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up tenant context for request");
            
            // Continue with request even if tenant setup fails
            // This ensures the application doesn't break due to tenant resolution issues
        }

        await _next(context);
    }

    private async Task SetupTenantContextAsync(HttpContext context, ITenantContext tenantContext, UserRepository userRepository)
    {
        // If multi-tenancy is disabled, set global tenant and continue
        if (!_options.Enabled)
        {
            tenantContext.SetCurrentTenant(_options.GlobalTenantId);
            _logger.LogDebug("Multi-tenancy disabled, using global tenant {TenantId}", _options.GlobalTenantId);
            
            // Still set up user context if available
            await SetupUserContextAsync(context, tenantContext, userRepository);
            return;
        }

        // Extract user information first
        await SetupUserContextAsync(context, tenantContext, userRepository);

        // Try to extract tenant from various sources
        var tenantId = await ExtractTenantIdAsync(context, tenantContext);

        if (tenantId.HasValue)
        {
            tenantContext.SetCurrentTenant(tenantId.Value);
            _logger.LogDebug("Set tenant context to {TenantId} for request {RequestPath}", 
                tenantId.Value, context.Request.Path);
        }
        else if (_options.RequireExplicitTenant)
        {
            _logger.LogWarning("No tenant specified for request {RequestPath} and explicit tenant required", 
                context.Request.Path);
            
            // Optionally return 400 Bad Request if tenant is required
            // context.Response.StatusCode = 400;
            // await context.Response.WriteAsync("Tenant identifier required");
            // return;
        }
        else
        {
            // Use default tenant if no explicit tenant and not required
            tenantContext.SetCurrentTenant(_options.DefaultTenantId ?? _options.GlobalTenantId);
            _logger.LogDebug("No tenant specified, using default tenant {TenantId}", 
                _options.DefaultTenantId ?? _options.GlobalTenantId);
        }
    }

    private async Task SetupUserContextAsync(HttpContext context, ITenantContext tenantContext, UserRepository userRepository)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) 
                         ?? context.User.FindFirst("sub") 
                         ?? context.User.FindFirst("user_id");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogDebug("No valid user ID found in claims");
            return;
        }

        try
        {
            // Load user from repository
            var (user, _) = await userRepository.LoadAsync(userId);
            if (user != null)
            {
                tenantContext.SetCurrentUser(user);
                _logger.LogDebug("Set user context to {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("User {UserId} not found in repository", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user {UserId} from repository", userId);
        }
    }

    private async Task<Guid?> ExtractTenantIdAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Priority 1: Tenant already set in context (from user claims)
        if (tenantContext.CurrentTenantId.HasValue)
        {
            return tenantContext.CurrentTenantId;
        }

        // Priority 2: Extract from JWT claims
        var tenantFromClaims = ExtractTenantFromClaims(context);
        if (tenantFromClaims.HasValue)
        {
            return tenantFromClaims;
        }

        // Priority 3: Extract from HTTP headers (if allowed)
        if (_options.AllowTenantSwitchingViaHeaders)
        {
            var tenantFromHeaders = ExtractTenantFromHeaders(context);
            if (tenantFromHeaders.HasValue)
            {
                return tenantFromHeaders;
            }
        }

        // Priority 4: Extract from route/query parameters
        var tenantFromRoute = ExtractTenantFromRoute(context);
        if (tenantFromRoute.HasValue)
        {
            return tenantFromRoute;
        }

        return null;
    }

    private Guid? ExtractTenantFromClaims(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try tenant ID claim
        var tenantIdClaim = context.User.FindFirst(_options.TenantClaimName);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            _logger.LogDebug("Extracted tenant {TenantId} from JWT claim {ClaimName}", 
                tenantId, _options.TenantClaimName);
            return tenantId;
        }

        // Try tenant name claim (requires async lookup, which we can't do here)
        // This will be handled by TenantContext.SetCurrentUser
        var tenantNameClaim = context.User.FindFirst(_options.TenantNameClaimName);
        if (tenantNameClaim != null)
        {
            _logger.LogDebug("Found tenant name claim {TenantName}, will resolve asynchronously", 
                tenantNameClaim.Value);
        }

        return null;
    }

    private Guid? ExtractTenantFromHeaders(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(_options.TenantHeaderName, out var headerValues))
        {
            return null;
        }

        var headerValue = headerValues.FirstOrDefault();
        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }

        if (Guid.TryParse(headerValue, out var tenantId))
        {
            _logger.LogDebug("Extracted tenant {TenantId} from header {HeaderName}", 
                tenantId, _options.TenantHeaderName);
            return tenantId;
        }

        _logger.LogWarning("Invalid tenant ID in header {HeaderName}: {HeaderValue}", 
            _options.TenantHeaderName, headerValue);
        return null;
    }

    private Guid? ExtractTenantFromRoute(HttpContext context)
    {
        // Try route values
        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId) && 
            routeTenantId != null && 
            Guid.TryParse(routeTenantId.ToString(), out var tenantFromRoute))
        {
            _logger.LogDebug("Extracted tenant {TenantId} from route parameter", tenantFromRoute);
            return tenantFromRoute;
        }

        // Try query parameters
        if (context.Request.Query.TryGetValue("tenantId", out var queryTenantId) && 
            Guid.TryParse(queryTenantId.FirstOrDefault(), out var tenantFromQuery))
        {
            _logger.LogDebug("Extracted tenant {TenantId} from query parameter", tenantFromQuery);
            return tenantFromQuery;
        }

        return null;
    }
}