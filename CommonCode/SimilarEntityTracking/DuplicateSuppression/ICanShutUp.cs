namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;



/// <summary>
/// ICanShutUp is the interface an object needs to inherit from in order to be be suppressible. 
/// </summary>
public interface ICanShutUp: ITrackSimilar
{
    /// <summary>
    /// SuppressionTimeMinutes describes the length of time that an inherited object will be suppressed for. 
    /// </summary>
    public int SuppressionTimeMinutes { get; }

}

