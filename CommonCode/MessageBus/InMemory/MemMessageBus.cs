using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
/// MemMessageBus implements IMessageBusSpecifier for creating and getting exchanges defined in the current message bus. 
///     -Usage(Injected with IMessageBusSpecifier):
///         >MemMessageListener.cs
///         >MemMessagePublisher.cs
///         >MemMessageRetreiver.cs
///         >AppEventBridge.cs
///         >AppEventPump.cs
///         >UserActionCompletion.cs
///         >NotificationService.cs
///         >RequestNotification.cs
///         >UserToastLogic.cs
///         >ConsolidateToDigestToDigestOrder.cs
///         >ConsolidateService.cs
///         >DigestResultReceiver.cs
///         >DuplicateSuppressionService.cs
///         >SuppressionOrder.cs
///         >SuppressionResultReceiver.cs
///     -Functions:
///         >DeclareExchange:
///         >DeleteExchange:
///         >SpecifyExchange:
///         >Dispose:
/// </summary>
public class MemMessageBus : IMessageBusSpecifier
{
    private static readonly ConcurrentDictionary<string, MemExchange> Exchanges =
        new();

    private bool _isDisposed;

    public MemMessageBus()
    {
    }

    #region IMessageBusSpecifier Members

    public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
    {
        exchangeName.Requires().IsNotNullOrEmpty();

        if (!Exchanges.ContainsKey(exchangeName))
        {
            Exchanges.TryAdd(exchangeName,
                             new MemExchange(new MessageExchangeDeclaration
                                             { Name = exchangeName, Type = exchangeType }));
        }
        return this;
    }

    public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
    {
        return DeclareExchange(exchangeName.EnumName(), exchangeType);
    }

    public IMessageBusSpecifier DeleteExchange(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();
        Exchanges.TryRemove(exchangeName, out _);
        return this;
    }

    public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
    {
        return DeleteExchange(exchangeName.EnumName());
    }

    public IExchangeSpecifier SpecifyExchange(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();
        bool found = Exchanges.TryGetValue(exchangeName, out MemExchange? me);
        found.Guarantees().IsTrue();
        return me!;
    }

    public IExchangeSpecifier SpecifyExchange(Enum exchangeName)
    {
        return SpecifyExchange(exchangeName.EnumName());
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    
}
