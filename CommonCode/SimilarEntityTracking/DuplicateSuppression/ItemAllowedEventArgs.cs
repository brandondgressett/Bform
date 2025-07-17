namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// ItemAllowedEventArgs describes event handling information for objects that will not be suppressed.
/// </summary>
/// <typeparam name="T">Instance of ICanShutUp</typeparam>
public class ItemAllowedEventArgs<T>: EventArgs
    where T: class, ICanShutUp, new()
{
    /// <summary>
    /// Item describes an instance of ICanShutUp that will not be suppressed.
    /// </summary>
    public T? Item { get; set; }
}
