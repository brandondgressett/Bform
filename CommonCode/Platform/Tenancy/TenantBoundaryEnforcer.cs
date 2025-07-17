using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Enforces tenant boundaries to prevent cross-tenant data access and operations.
/// Provides validation and enforcement of tenant isolation rules.
/// </summary>
public class TenantBoundaryEnforcer
{
    private readonly ITenantContext _tenantContext;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantBoundaryEnforcer> _logger;

    public TenantBoundaryEnforcer(
        ITenantContext tenantContext,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantBoundaryEnforcer> logger)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that an event can be processed by the current tenant context.
    /// </summary>
    /// <param name="appEvent">The event to validate</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>True if the event can be processed, false otherwise</returns>
    public bool ValidateEventAccess(AppEvent appEvent, string operationName)
    {
        if (!_options.Enabled)
        {
            return true; // No enforcement when multi-tenancy is disabled
        }

        var currentTenantId = _tenantContext.TenantId;
        var eventTenantId = appEvent.TenantId.ToString();

        // Allow system-level operations (no tenant context)
        if (string.IsNullOrEmpty(currentTenantId) && string.IsNullOrEmpty(eventTenantId))
        {
            return true;
        }

        // Deny access if tenant IDs don't match
        if (currentTenantId != eventTenantId)
        {
            _logger.LogWarning(
                "Tenant boundary violation in {Operation}: current tenant {CurrentTenant} attempted to access event {EventId} from tenant {EventTenant}",
                operationName, currentTenantId, appEvent.Id, eventTenantId);
            
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that an entity belongs to the current tenant.
    /// </summary>
    /// <param name="entityTenantId">The tenant ID of the entity</param>
    /// <param name="entityName">Name of the entity for logging</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>True if the entity can be accessed, false otherwise</returns>
    public bool ValidateEntityAccess(string? entityTenantId, string entityName, string operationName)
    {
        if (!_options.Enabled)
        {
            return true; // No enforcement when multi-tenancy is disabled
        }

        var currentTenantId = _tenantContext.TenantId;

        // Allow system-level operations (no tenant context)
        if (string.IsNullOrEmpty(currentTenantId) && string.IsNullOrEmpty(entityTenantId))
        {
            return true;
        }

        // Deny access if tenant IDs don't match
        if (currentTenantId != entityTenantId)
        {
            _logger.LogWarning(
                "Tenant boundary violation in {Operation}: current tenant {CurrentTenant} attempted to access entity {EntityName} from tenant {EntityTenant}",
                operationName, currentTenantId, entityName, entityTenantId);
            
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ensures that a new entity will be created with the correct tenant ID.
    /// </summary>
    /// <param name="entity">The entity to validate and potentially update</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>True if the entity was validated successfully, false if there was a violation</returns>
    public bool EnsureEntityTenantContext(IAppEntity entity, string operationName)
    {
        if (!_options.Enabled)
        {
            return true; // No enforcement when multi-tenancy is disabled
        }

        var currentTenantId = _tenantContext.TenantId;

        // For system-level operations, allow entities without tenant context
        if (string.IsNullOrEmpty(currentTenantId))
        {
            return true;
        }

        // If entity doesn't have a tenant ID, assign the current tenant
        if (string.IsNullOrEmpty(entity.TenantId))
        {
            entity.TenantId = currentTenantId;
            _logger.LogDebug(
                "Assigned tenant {TenantId} to entity {EntityType} in operation {Operation}",
                currentTenantId, entity.GetType().Name, operationName);
            return true;
        }

        // If entity has a tenant ID, verify it matches current tenant
        if (entity.TenantId != currentTenantId)
        {
            _logger.LogError(
                "Tenant boundary violation in {Operation}: current tenant {CurrentTenant} attempted to create/modify entity with tenant {EntityTenant}",
                operationName, currentTenantId, entity.TenantId);
            
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a TenantBoundaryViolationException for tenant boundary violations.
    /// </summary>
    /// <param name="operationName">Name of the operation where violation occurred</param>
    /// <param name="currentTenantId">Current tenant ID</param>
    /// <param name="attemptedTenantId">Tenant ID that was attempted to be accessed</param>
    /// <returns>Exception ready to be thrown</returns>
    public TenantBoundaryViolationException CreateViolationException(
        string operationName, 
        string? currentTenantId, 
        string? attemptedTenantId)
    {
        var message = $"Tenant boundary violation in {operationName}: " +
                     $"current tenant '{currentTenantId}' cannot access resources from tenant '{attemptedTenantId}'";
        
        return new TenantBoundaryViolationException(message, currentTenantId, attemptedTenantId);
    }

    /// <summary>
    /// Throws a TenantBoundaryViolationException if the condition is met.
    /// </summary>
    /// <param name="condition">Condition that triggers the exception</param>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="currentTenantId">Current tenant ID</param>
    /// <param name="attemptedTenantId">Attempted tenant ID</param>
    public void ThrowIfViolation(
        bool condition, 
        string operationName, 
        string? currentTenantId, 
        string? attemptedTenantId)
    {
        if (condition)
        {
            throw CreateViolationException(operationName, currentTenantId, attemptedTenantId);
        }
    }
}