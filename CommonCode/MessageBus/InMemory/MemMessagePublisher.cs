using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.MessageBus.InMemory;

/// <summary>
/// MemMessagePublisher implements Send() from IMessagePublisher to publish messages to the message bus defined. 
///     -Usage (Injected with IMessagePublisher):
///         >AppEventPump.cs
///         >UserActionCompletion.cs
///         >RequestNotifiction.cs
///         >UserToastLogic.cs
///         >ConsolidateToDigestOrder.cs
///         >DigestDistributionService.cs
///         >DuplicateSuppressionService.cs
///         >SuppressionOrder.cs
///     -Functions:
///         >Initialize: 
///         >Send: 
///         >SendAsync: 
///         >RefreshQueues: 
///         >Dispose: 
/// </summary>
public class MemMessagePublisher : IMessagePublisher
{
    private readonly IMessageBusSpecifier _bus;
    private string? _exchangeName;
    private ExchangeTypes _exchangeType;
    private bool _isDisposed;
    private readonly ConcurrentBag<IQueueSpecifier> _queues = new();
    private readonly ILogger<MemMessagePublisher> _logger;


    public MemMessagePublisher(IMessageBusSpecifier bus, ILogger<MemMessagePublisher> log)
    {
        _logger = log;
        _bus = bus;
    }
    
    #region IMessagePublisher Members

    public void Initialize(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();
        _exchangeName = exchangeName;
    }

    public void Send<T>(T msg, string routeKey) 
    {
        routeKey.Requires().IsNotNullOrEmpty();
        if(msg is null) throw new ArgumentNullException(nameof(msg));
        
        RefreshQueues();

        var matchingQueues = MessageExchangeDeclaration.BindMessageToQueues(routeKey, _exchangeType, _queues);

#if DEBUG
        if(matchingQueues.Count() == 0)
        {
            _logger.LogInformation("Sending message to exchange {ex}, route {rt}, fonud no matching queues.", _exchangeName, routeKey);
        }
#endif

        foreach (var q in matchingQueues)
        {
            var queue = _queues.FirstOrDefault(s => s.Name == q);
            var outbox = queue as IMemQueueAccess;
            if (outbox is not null)
            {
#if DEBUG
                _logger.LogInformation("Sending message to exchange {ex}, route {rt}, message:{m}",
                    _exchangeName, routeKey, JsonConvert.SerializeObject(msg, Formatting.Indented));
#endif
                outbox.Queue.Enqueue(new LightMessageQueueEnvelope(msg));
                outbox.SentEvent.Set();
            } else
            {
#if DEBUG
                _logger.LogInformation("No queue matching {q}! attempting to send message to exchange {ex}, route {rt}, message:{m}",
                    q, _exchangeName, routeKey, JsonConvert.SerializeObject(msg, Formatting.Indented));
#endif
            }
        }
    }

    public void Send<T>(T msg, Enum routeKey) 
    {
        Send(msg, routeKey.EnumName());
    }

    public Task SendAsync<T>(T msg, Enum routeKey)
    {
        Send(msg, routeKey);
        return Task.CompletedTask;
    }

    public Task SendAsync<T>(T msg, string routeKey)
    {
        Send(msg, routeKey);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _bus.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    private void RefreshQueues()
    {
        _exchangeName.Requires().IsNotNullOrEmpty();
        var ex = _bus.SpecifyExchange(_exchangeName!);
        if (null != ex)
        {
            _queues.Clear();
            foreach(var q in ex.Queues)
                _queues.Add(q); 
            _exchangeType = ex.ExchangeType;
        }
        else
        {
            _queues.Clear();
        }
    }
}
