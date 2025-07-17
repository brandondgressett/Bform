namespace BFormDomain.MessageBus;

/// <summary>
///     Defines a message exchange, declares queues
/// </summary>
public interface IExchangeSpecifier
{
    /// <summary>
    ///     Name of the exchange
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Type of the exchange, defining its message routing behavior
    /// </summary>
    ExchangeTypes ExchangeType { get; }

    IEnumerable<IQueueSpecifier> Queues { get; }

    /// <summary>
    ///     Declares a message queue in this exchange
    /// </summary>
    /// <param name="queueName"> the queue name </param>
    /// <param name="boundRoutes"> the queue route parameters </param>
    /// <returns> this </returns>
    IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes);

    /// <summary>
    ///     Declares a message queue in this exchange
    /// </summary>
    /// <param name="queueName"> the queue name </param>
    /// <param name="boundRoutes"> the queue route parameters </param>
    /// <returns> this </returns>
    IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes);


    /// <summary>
    ///     Deletes a queue from the exchange
    /// </summary>
    /// <param name="queueName"> Name of the queue to delete </param>
    /// <returns> this </returns>
    IExchangeSpecifier DeleteQueue(string queueName);

    /// <summary>
    ///     Deletes a queue from the exchange
    /// </summary>
    /// <param name="queueName"> Name of the queue to delete </param>
    /// <returns> this </returns>
    IExchangeSpecifier DeleteQueue(Enum queueName);

    /// <summary>
    ///     Accesses the queue specifier
    /// </summary>
    /// <param name="queueName"> name of the queue </param>
    /// <returns> a queue specifier </returns>
    IQueueSpecifier SpecifyQueue(string queueName);

    /// <summary>
    ///     Accesses the queue specifier
    /// </summary>
    /// <param name="queueName"> name of the queue </param>
    /// <returns> a queue specifier </returns>
    IQueueSpecifier SpecifyQueue(Enum queueName);
}


