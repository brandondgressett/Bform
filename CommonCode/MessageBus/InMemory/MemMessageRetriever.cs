using BFormDomain.Validation;
using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
/// MemMessageRetriever implements MaybeGetMessageAsync from IMessageRetriever to retrieve messages from the defined message bus when requested. 
///     -References:
///         >
///     -Functions:
///         >
/// </summary>
public class MemMessageRetriever : IMessageRetriever
{
    private readonly IMessageBusSpecifier _bus;
    private ConcurrentQueue<LightMessageQueueEnvelope>? _q;
    private bool _isDisposed = false;

    public MemMessageRetriever(IMessageBusSpecifier bus)
    {
        _bus = bus;
    }

    

    public void Initialize(string exchangeName, string qName)
    {
        var qs = _bus.SpecifyExchange(exchangeName).SpecifyQueue(qName);
        _q = ((IMemQueueAccess)qs).Queue;
    }

    public Task<MessageContext<T>?>MaybeGetMessageAsync<T>() where T : class, new()
    {
        _q!.Requires("Message Queue Retriever NOT initialized before listening").IsNotNull();
        MessageContext<T>? retval = null;

        if (_q!.TryDequeue(out LightMessageQueueEnvelope? env))
        {
            var ack = new MemQueueAcknowledge(_q, env);
            T? item = env.Decode() as T;
            if(item is not null)
                retval = new(item, ack);
        }

        return Task.FromResult(retval);
        
    }
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
