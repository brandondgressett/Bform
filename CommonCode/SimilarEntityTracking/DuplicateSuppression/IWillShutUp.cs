namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// IWillShutUp can be inherited from to allow objects to be suppressed at a specific time. 
/// </summary>
public interface IWillShutUp: ICanShutUp
{
    /// <summary>
    /// SuppressionStartTime describes the date and time in which to start suppressing the inherited object. 
    /// </summary>
    public DateTime SuppressionStartTime { get; set; }
}


