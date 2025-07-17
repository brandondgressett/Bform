namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Exception thrown when a tenant boundary violation is detected.
/// This indicates an attempt to access data or perform operations 
/// across tenant boundaries in violation of multi-tenancy isolation rules.
/// </summary>
public class TenantBoundaryViolationException : Exception
{
    /// <summary>
    /// The current tenant ID that attempted the operation.
    /// </summary>
    public string? CurrentTenantId { get; }

    /// <summary>
    /// The tenant ID that was attempted to be accessed.
    /// </summary>
    public string? AttemptedTenantId { get; }

    public TenantBoundaryViolationException(string message) : base(message)
    {
    }

    public TenantBoundaryViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public TenantBoundaryViolationException(
        string message, 
        string? currentTenantId, 
        string? attemptedTenantId) : base(message)
    {
        CurrentTenantId = currentTenantId;
        AttemptedTenantId = attemptedTenantId;
    }

    public TenantBoundaryViolationException(
        string message, 
        string? currentTenantId, 
        string? attemptedTenantId, 
        Exception innerException) : base(message, innerException)
    {
        CurrentTenantId = currentTenantId;
        AttemptedTenantId = attemptedTenantId;
    }
}