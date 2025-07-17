using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using BFormDomain.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// -DuplicateSuppressionService listens for instances of SuppressDuplicatesMessage via an injected message bus listener 
/// -Sends messages to message buses for suppressed items or allowed items listened for in SuppressionResultReceiver via an injected message bus publisher factory
/// </summary>
/// <typeparam name="T">T can be any type that implements the ICanShutUp interface</typeparam>
public class DuplicateSuppressionService<T> : IHostedService, IDisposable
    where T: class, ICanShutUp, new()
{

    #region fields
    /// <summary>
    /// _qListener is an injected instance that uses exchange name to listen for items off of the message bus specified.
    /// </summary>
    private IMessageListener? _qListener;

    /// <summary>
    /// _busSpec is an injected instance containing exchange/ route names for message bus.
    /// </summary>
    private readonly IMessageBusSpecifier _busSpec;

    /// <summary>
    /// _pubFactory is an injected instance that uses exchange name passed into initialize function to publish items to the message bus specified.
    /// </summary>
    private readonly KeyInject<string, IMessagePublisher>.ServiceResolver _pubFactory;

    /// <summary>
    /// _core is used to pass each message from the message bus listener into ShouldBeSuppressed() to determine if the message should be suppressed. 
    /// </summary>
    private readonly DuplicateSuppressionCore<T> _core;

    /// <summary>
    /// _logger is an injected instance used to log information specified. 
    /// </summary>
    private readonly ILogger<DuplicateSuppressionService<T>> _logger;

    /// <summary>
    /// _alerts is an injected instance used to generate alerts upon catching exceptions. 
    /// </summary>
    private readonly IApplicationAlert _alerts;

    /// <summary>
    /// _exchangeName is used to set exchange for message bus specifier
    /// </summary>
    private readonly string _exchangeName;

    /// <summary>
    /// _qName is the name of the message bus listener
    /// </summary>
    private readonly string _qName;

    /// <summary>
    /// _ct is used before proceeding through ProcessMessage() to determine if the request for suppression was cancelled or not. 
    /// </summary>
    private CancellationToken _ct;
    #endregion

    /// <summary>
    /// DI Constructor. Register as transient.
    /// </summary>
    /// <param name="pubFactory"></param>
    /// <param name="listener"></param>
    /// <param name="busSpec"></param>
    /// <param name="core"></param>
    /// <param name="logger"></param>
    /// <param name="alerts"></param>
    public DuplicateSuppressionService(KeyInject<string, IMessagePublisher>.ServiceResolver pubFactory,
                                       KeyInject<string, IMessageListener>.ServiceResolver  listener,
                                       KeyInject<string, IMessageBusSpecifier>.ServiceResolver busSpec,   
                                       DuplicateSuppressionCore<T> core,
                                       ILogger<DuplicateSuppressionService<T>> logger,
                                       IApplicationAlert alerts)
    {
        _pubFactory = pubFactory;
        _qListener = listener(MessageBusTopology.Distributed.EnumName());
        _busSpec = busSpec(MessageBusTopology.Distributed.EnumName());
        _core = core;
        _logger = logger;
        _alerts = alerts;
        
        
        _exchangeName = $"suppress_duplicates_{typeof(T).GetFriendlyTypeName()}";
        _qName = $"{typeof(T).GetFriendlyTypeName()}";
    }


    /// <summary>
    /// Dispose() is inherited from IDisposable and is used to dispose of objects that implement Dispose()
    /// </summary>
    public void Dispose()
    {
        _qListener?.Dispose();
        _qListener = null!;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Upon starting the service this function will initialize the message bus listener that listens for instances of SuppressDuplicatesMessage sent by SuppressionOrder
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ct = cancellationToken;

        if (_qListener is not null)
        {
            _busSpec
                .DeclareExchange(_exchangeName, ExchangeTypes.Direct)
                .SpecifyExchange(_exchangeName)
                .DeclareQueue(_qName, _qName);

            _logger.LogInformation($"Duplicate Suppression Service <{typeof(T).Name}> listening on {_exchangeName}.{_qName}");

            _qListener.Initialize(_exchangeName, _qName);
            _qListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>
                                    (typeof(SuppressDuplicatesMessage), ProcessMessage));
            
        }

        return Task.CompletedTask;

    }

    /// <summary>
    /// -ProcessMessage() is called for every message received on the message bus listener
    /// -Each message is passed into into _core.ShouldBeSuppressed to determine if message should be suppressed. 
    /// -Suppressed messages are sent to a message bus (listened for in SuppressionResultReceiver) specified by the messages SuppressedExchange.
    /// -Allowed messages are sent to a message bus (listened for in SuppressionResultReceiver) specified by the messages ForwardExchange.
    /// 
    /// 
    /// -The function:
    ///     -receives messages from the message bus listener
    ///     -uses ShouldBeSuppressed() to determine if the message is passed or suppressed
    ///     -publishes suppressed and allowed messages to different respective exchanges
    ///         -SuppressedExchange for suppressed messages (if the message property asks for forwarded suppressed items)
    ///         -ForwardExchange for allowed messages. 
    ///             -The allowed message's suppression time determines how long the system suppresses future matches for messages of this kind.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="ct"></param>
    /// <param name="ack"></param>
    private void ProcessMessage(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        var message = msg as SuppressDuplicatesMessage;
        if (message is not null && !_ct.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try
            {
                var item = message.SuppressedItem as T;
                item.Guarantees("Could not unbox suppressable item in duplicate suppression service").IsNotNull();

                var forwardPub = _pubFactory(MessageBusTopology.Distributed.EnumName());
                forwardPub.Guarantees("IMessagePublisher implementation must be registered with DI container.").IsNotNull();

                if (_core.ShouldBeSuppressed(item!).Result)
                {
                    // it's suppressed

                    // if the message has a suppressed item exchange send it there
                    if (!string.IsNullOrWhiteSpace(message.SuppressedExchange) && !string.IsNullOrWhiteSpace(message.SuppressedQueue))
                    {
                        forwardPub!.Initialize(message.SuppressedExchange);
                        forwardPub.Send(item, message.SuppressedQueue);
                        _logger.LogDebug($"Forwarding suppressed item {item!.ComparisonType} {item.TargetId} {item.ComparisonHash} to {message.SuppressedExchange}.{message.SuppressedQueue}");
                    } // otherwise, ignore it!
                }
                else // it's allowed
                {
                    forwardPub!.Initialize(message.ForwardExchange);
                    forwardPub.Send(item, message.ForwardQueue);
                    _logger.LogDebug($"Forwarding not suppressed item {item!.ComparisonType} {item.TargetId} {item.ComparisonHash} to {message.ForwardExchange}.{message.ForwardQueue}");

                }

                ack.MessageAcknowledged();

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, LogLevel.Information, ex.TraceInformation(), 5);
                ack.MessageRejected();
            }
        }
        else
            ack.MessageRejected();
    }

    /// <summary>
    /// Upon stopping the service will silence the message bus listener
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_qListener != null)
            _qListener.Paused = true;
        return Task.CompletedTask;
    }
}


