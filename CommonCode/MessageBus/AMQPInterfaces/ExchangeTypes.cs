namespace BFormDomain.MessageBus;

/// <summary>
///     Describes the types of message exchanges
/// </summary>
public enum ExchangeTypes
{
    /// <summary>
    ///     In a direct exchange, messages are routed by route key match to queue binding.
    /// </summary>
    Direct,

    /// <summary>
    ///     In a fanout exchange, each message is fed to every queue in the exchange
    /// </summary>
    Fanout,

    /// <summary>
    ///     In a topic exchange, the message route key is matched to wildcard queue bindings and routed to the appropriate queue
    /// </summary>
    Topic
}


