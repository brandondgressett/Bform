using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public class DigestResultReceiver<T>: IDisposable
    where T:class, new()
{
    private readonly IMessageBusSpecifier _messageBusSpecifier;
    private readonly IMessageListener _digestListener;
    private readonly ILogger<DigestResultReceiver<T>> _logger;

    public event EventHandler<DigestReadyEventArgs<T>>? DigestReady;

    public DigestResultReceiver(
            KeyInject<string,IMessageBusSpecifier>.ServiceResolver specifier, 
            KeyInject<string,IMessageListener>.ServiceResolver messageListener,
            ILogger<DigestResultReceiver<T>> logger)
    {
        _messageBusSpecifier = specifier(MessageBusTopology.Distributed.EnumName());
        _digestListener = messageListener(MessageBusTopology.Distributed.EnumName());

        _logger = logger;
    }

    public void Initialize(string? exchange = null, string? queue = null)
    {
        if(string.IsNullOrWhiteSpace(exchange) || string.IsNullOrWhiteSpace(queue))
        {
            exchange = $"consolidate_digest_{typeof(Digestible<T>).GetFriendlyTypeName()}";
            queue = $"dig_rcv_{typeof(Digestible<T>).GetFriendlyTypeName()}";
        }

        _messageBusSpecifier.DeclareExchange(exchange, ExchangeTypes.Direct)
                            .SpecifyExchange(exchange)
                            .DeclareQueue(queue, queue);

        _digestListener.Initialize(exchange, queue);
        _digestListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
            typeof(DigestModel), ProcessReceived));
    }

    private void ProcessReceived(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        try
        {
            if (null != DigestReady && msg is DigestModel item && item.Entries.Any())
            {
                var digest = item.Entries.Select(it => new DigestItem<T>
                {
                    DateTime = it.InvocationTime,

                    // the extra dependency on the mongodb c# driver here is ... unfortunate,
                    // but the simplest way to proceed.
                    Item = BsonSerializer.Deserialize<T>(it.Entry)
                })
                    .ToList();

                DigestReady?.Invoke(this, new DigestReadyEventArgs<T> { Digest = digest });
            }

            ack.MessageAcknowledged();
        } catch (Exception ex)
        {
            ack.MessageRejected();
            _logger.LogError("{trace}", ex.TraceInformation());
        }

    }

    public void Dispose()
    {
        _digestListener?.Dispose();
        GC.SuppressFinalize(this);
    }
}
