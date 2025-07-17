namespace BFormDomain.MessageBus;



/// <summary>
///     Specifies the exchanges in the message bus
/// </summary>
public interface IMessageBusSpecifier : IDisposable
{
    /// <summary>
    ///     Declares a message exchange
    /// </summary>
    /// <param name="exchangeName"> exchange name </param>
    /// <param name="exchangeType"> exchange type </param>
    /// <returns> this </returns>
    IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType);

    /// <summary>
    ///     Declares a message exchange
    /// </summary>
    /// <param name="exchangeName"> exchange name </param>
    /// <param name="exchangeType"> exchange type </param>
    /// <returns> </returns>
    IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType);

    /// <summary>
    ///     Deletes a message exchange
    /// </summary>
    /// <param name="exchangeName"> exchange name </param>
    /// <returns> this </returns>
    IMessageBusSpecifier DeleteExchange(string exchangeName);

    /// <summary>
    ///     Deletes a message exchange
    /// </summary>
    /// <param name="exchangeName"> exchange name </param>
    /// <returns> this </returns>
    IMessageBusSpecifier DeleteExchange(Enum exchangeName);

    /// <summary>
    ///     Provides access to specify exchange queues.
    /// </summary>
    /// <param name="exchangeName"> The exchange name. </param>
    /// <returns> An exchange specifier </returns>
    IExchangeSpecifier SpecifyExchange(string exchangeName);

    /// <summary>
    ///     Provides access to specify exchange queues.
    /// </summary>
    /// <param name="exchangeName"> The exchange name. </param>
    /// <returns> An exchange specifier </returns>
    IExchangeSpecifier SpecifyExchange(Enum exchangeName);
}


