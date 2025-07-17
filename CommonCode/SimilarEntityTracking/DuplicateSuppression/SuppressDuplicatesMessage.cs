namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// SuppressDuplicatesMessage describes message bus information for allowed and suppressed items
///     Used by DuplicateSuppressionService to determine what message buses to send allowed and suppressed items.
/// </summary>
public class SuppressDuplicatesMessage
{
    /// <summary>
    /// SuppressedItem is the object being suppressed. 
    /// </summary>
    public object SuppressedItem { get; set; } = null!;

    /// <summary>
    /// ForwardExchange describes the exhange name to initialize before sending allowed items to message bus.
    /// </summary>
    public string ForwardExchange { get; set; } = null!;

    /// <summary>
    /// ForwardQueue describes the message bus name to send allowed items.
    /// </summary>
    public string ForwardQueue { get; set; } = null!;

    /// <summary>
    /// SuppressedExchange describes the exhange name to initialize before sending suppressed items to message bus.
    /// </summary>
    public string? SuppressedExchange { get; set; }

    /// <summary>
    /// SuppressedQueue describes the message bus name to send suppressed items.
    /// </summary>
    public string? SuppressedQueue { get; set; }
}


