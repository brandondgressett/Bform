using BFormDomain.HelperClasses;
using BFormDomain.Validation;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Suppressible<T> : ICanShutUp
    where T : class, new()
{
    /// <summary>
    /// 
    /// </summary>
    public Suppressible()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="suppressionMinutes"></param>
    /// <param name="itemId"></param>
    /// <param name="props"></param>
    public Suppressible(T item,
        int suppressionMinutes,
        string itemId,
        params Func<T,string>[] props)
    {
        item.Requires().IsNotNull();
        suppressionMinutes.Requires().IsGreaterOrEqual(0);
        itemId.Requires().IsNotNullOrEmpty();
        props.Requires().IsNotEmpty();

        Item = item;
        SuppressionTimeMinutes = suppressionMinutes;
        TargetId = itemId;
        ComparisonType = typeof(T).GetFriendlyTypeName();
        ComparisonPropertyString = String.Join(',', props.Select(fn => fn(item)));
        ComparisonHash = ComparisonPropertyString.GetHashCode();
    }

    #region Properties
    /// <summary>
    /// 
    /// </summary>
    public int SuppressionTimeMinutes { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string TargetId { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    public string ComparisonType { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    public long ComparisonHash { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string ComparisonPropertyString { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    public T? Item { get; set; }
    #endregion
}
