namespace BFormDomain.MessageBus;

// message queueing based on AMQP constructs





/// <summary>
///     This interface is sent to message listeners, so that they may call back and release control over a message
/// </summary>
public interface IMessageAcknowledge
{
    /// <summary>
    ///     If a listener calls this method, it means that it has processed the queue message and it may be taken off of the queue.
    /// </summary>
    void MessageAcknowledged();


    /// <summary>
    ///     If a listener calls this method, it is to be kept on the queue.
    /// </summary>
    void MessageAbandoned();

    /// <summary>
    ///     If a listen calls this method, then the message cannot be processed.
    /// </summary>
    void MessageRejected();
}


