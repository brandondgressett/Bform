namespace BFormDomain.MessageBus;

/// <summary>
///     Listener objects received queue messages, filtered by message type
/// </summary>
public interface IMessageListener : IDisposable
{
    void Initialize(string exchangeName, string qName);
    /// <summary>
    ///     Registers a listener of a message queue
    /// </summary>
    /// <param name="listener"> Filtering by message type, a delegate that processes a message </param>
    void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener);

    bool Paused { get; set; }

    event EventHandler<IEnumerable<object>> ListenAborted;
}


