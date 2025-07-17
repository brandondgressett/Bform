namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
public interface ISuppressionPersistence   
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Task<IWillShutUp?> GetSuppressionInfo(ICanShutUp item);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Task SuppressStartingNow(ICanShutUp item);

}


