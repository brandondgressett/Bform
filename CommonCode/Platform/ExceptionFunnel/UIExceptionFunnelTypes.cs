namespace BFormDomain.CommonCode.Platform;


/// <summary>
/// UIExceptionFunnelTypes represents a list of the domains for exception types.
/// </summary>
public enum UIExceptionFunnelTypes
{
    /// <summary>
    /// API based funnels.
    /// </summary>
    WebAPI,

    /// <summary>
    /// Mongo based funnels.
    /// </summary>
    Repository,

    /// <summary>
    /// Validation based funnels.
    /// </summary>
    Validation,

}
