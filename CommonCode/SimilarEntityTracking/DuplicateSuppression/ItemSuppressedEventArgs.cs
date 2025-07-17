namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// ItemSuppressedEventArgs describes event handling information for objects that will be suppressed.
/// </summary>
/// <typeparam name="T">Instance of ICanShutUp</typeparam>
public class ItemSuppressedEventArgs<T> : EventArgs
    where T : class, ICanShutUp, new()
{
    /// <summary>
    /// Item describes an instance of ICanShutUp that will be suppressed.
    /// </summary>
    public T? Item { get; set; }
}
