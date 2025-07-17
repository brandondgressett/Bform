namespace BFormDomain.MessageBus;

/// <summary>
///     Defines a message queue
/// </summary>
public interface IQueueSpecifier
{
    /// <summary>
    ///     The name of the queue
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Queue message bindings. In a direct exchange, messages with matching keys are put in this queue. 
    ///     In a fanout exchange, bindings are ignored, and this queue will receive all messages. 
    ///     In a topic exchange, the bindings are wildcard strings that describe which messages the queue will receive
    /// </summary>
    IEnumerable<string> Bindings { get; }
}


