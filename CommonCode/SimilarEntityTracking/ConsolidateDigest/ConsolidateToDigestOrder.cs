using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public class ConsolidateToDigestOrder<T>
    where T: class, IDigestible, new()
{

    private readonly IMessagePublisher _messagePublisher;
    private readonly IMessageBusSpecifier _messageBusSpecifier;
    private bool _isInitialized = false;
    private readonly object _initLock = new();
    private readonly string _exchangeName;
    private readonly string _routeName;

    public ConsolidateToDigestOrder(
        KeyInject<string,IMessageBusSpecifier>.ServiceResolver messageBusSpecifier, 
        KeyInject<string,IMessagePublisher>.ServiceResolver publisher)
    {
        _messageBusSpecifier = messageBusSpecifier(MessageBusTopology.Distributed.EnumName());
        _messagePublisher = publisher(MessageBusTopology.Distributed.EnumName());

        _exchangeName = _exchangeName = $"consolidate_digest_{typeof(T).GetFriendlyTypeName()}";
        _routeName = $"{typeof(T).GetFriendlyTypeName()}";

    }

    public string Exchange => _exchangeName;
    public string Route => _routeName;
    
    private void MaybeInitialize()
    {
        lock(_initLock)
        {
            if (!_isInitialized)
            {
                _messageBusSpecifier
                            .DeclareExchange(_exchangeName, ExchangeTypes.Direct)
                            .SpecifyExchange(_exchangeName)
                            .DeclareQueue(_routeName, _routeName);

                _messagePublisher.Initialize(_exchangeName);

                _isInitialized = true;
            }
        }
    }

    public void ConsolidateIntoDigest(T item)
    {
        MaybeInitialize();
        
        item.ForwardToExchange = _exchangeName;
        item.ForwardToRoute = _routeName;
        var consDigMes = new ConsolidateDigestMessage(item);
        _messagePublisher.Send(consDigMes, _routeName);
    }

    public async Task ConsolidateIntoDigestAsync(T item)
    {
        MaybeInitialize();
        item.ForwardToExchange = _exchangeName;
        item.ForwardToRoute = _routeName;
        var consDigMes = new ConsolidateDigestMessage(item);
        await _messagePublisher.SendAsync(consDigMes, _routeName);
    }
}
