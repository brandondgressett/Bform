namespace BFormDomain.MessageBus;

/// <summary>
///     Publisher objects send messages onto a queue
/// </summary>
public interface IMessagePublisher : IDisposable
{
    void Initialize(string exchangeName);

    /// <summary>
    ///     sends a message onto a queue
    /// </summary>
    /// <typeparam name="T"> Message class type </typeparam>
    /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
    /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
    void Send<T>(T msg, string routeKey);

    /// <summary>
    ///     sends a message onto a queue
    /// </summary>
    /// <typeparam name="T"> Message class type </typeparam>
    /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
    /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
    void Send<T>(T msg, Enum routeKey);


    /// <summary>
    ///     sends a message onto a queue
    /// </summary>
    /// <typeparam name="T"> Message class type </typeparam>
    /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
    /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
    Task SendAsync<T>(T msg, string routeKey);

    /// <summary>
    ///     sends a message onto a queue
    /// </summary>
    /// <typeparam name="T"> Message class type </typeparam>
    /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
    /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
    Task SendAsync<T>(T msg, Enum routeKey);
}


