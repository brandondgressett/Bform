using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Tenancy;
using BFormDomain.Repository;

namespace BFormDomain.CommonCode.Platform.Rules;

/// <summary>
/// Provides context information for rule action execution, including
/// tenant awareness and transaction context.
/// </summary>
public class RuleActionContext
{
    /// <summary>
    /// The transaction context for data operations.
    /// </summary>
    public ITransactionContext TransactionContext { get; set; } = null!;

    /// <summary>
    /// The app event that triggered the rule.
    /// </summary>
    public AppEvent AppEvent { get; set; } = null!;

    /// <summary>
    /// The tenant ID from the triggering event.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The current tenant context for accessing tenant-specific services.
    /// </summary>
    public ITenantContext TenantContext { get; set; } = null!;

    /// <summary>
    /// Additional metadata that can be passed between rule actions.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}