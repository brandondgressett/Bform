using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.MessageBus;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;


/// <summary>
/// -SuppressionResultReceiver listens for messages sent by the DuplicateSuppressionService to process suppressed and allowed messages
///     -Initializes suppressed and allowed message bus listeners
///     -Passes allowed messages through ProcessAllowed() to be handled by the definition of ItemAllowed defined by client
///     -Passes suppressed messages through ProcessSuppressed() to be handled by the definition of ItemSuppressed defined by client
/// </summary>
/// <typeparam name="T"></typeparam>
public class SuppressionResultReceiver<T>: IDisposable
    where T: class, ICanShutUp, new()
{
    #region Fields
    /// <summary>
    /// _messageBusSpecifier is an injected instance containing exchange/ route names for message bus.
    /// </summary>
    private readonly IMessageBusSpecifier _messageBusSpecifier;

    /// <summary>
    /// _suppressedListener and _allowedListeneris are injected instances that use exchange names to listen for items off of the message bus specified.
    /// </summary>
    private readonly IMessageListener _suppressedListener, _allowedListener;

    /// <summary>
    /// _logger is an injected instance used to log information specified.
    /// </summary>
    private readonly ILogger<SuppressionResultReceiver<T>> _logger;
    #endregion

    #region Events

    /// <summary>
    /// ItemSuppressed is invoked by ProcessSupressed() to handle suppressed messages however the client defined them.
    /// </summary>
    public event EventHandler<ItemSuppressedEventArgs<T>>? ItemSuppressed;

    /// <summary>
    /// ItemAllowed is invoked by ProcessALlowed() to handle allowed messages however the client defined them.
    /// </summary>
    public event EventHandler<ItemAllowedEventArgs<T>>? ItemAllowed;
    #endregion

    /// <summary>
    /// DI Constructor. Register as transient.
    /// </summary>
    /// <param name="listenerFactory"></param>
    /// <param name="specifier"></param>
    /// <param name="logger"></param>
    public SuppressionResultReceiver(
        KeyInject<string,IMessageListener>.ServiceResolver listenerFactory, 
        KeyInject<string,IMessageBusSpecifier>.ServiceResolver specifier, 
        ILogger<SuppressionResultReceiver<T>> logger)
    {
        _allowedListener = listenerFactory(MessageBusTopology.Distributed.EnumName());
        _suppressedListener = listenerFactory(MessageBusTopology.Distributed.EnumName());
        _messageBusSpecifier = specifier(MessageBusTopology.Distributed.EnumName());
        _logger = logger;

    }

    /// <summary>
    /// Dispose() calls Dispose on all disposable objects in the class.
    /// </summary>
    public void Dispose()
    {
        _allowedListener?.Dispose();
        _suppressedListener?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Initialize() initializes the allowed and suppressed message bus listeners. 
    /// </summary>
    /// <param name="receiveSuppressedToo"></param>
    public void Initialize(bool receiveSuppressedToo = false)
    {
        try
        {
            #region allowed stuff
            var forwardedExchange = $"rcv_passed_suppression_{typeof(T).GetFriendlyTypeName()}";
            var forwardedQueue = $"q_passed_suppression_{typeof(T).GetFriendlyTypeName()}";

            _messageBusSpecifier.DeclareExchange(forwardedExchange, ExchangeTypes.Direct)
                                .SpecifyExchange(forwardedExchange)
                                .DeclareQueue(forwardedQueue, forwardedQueue);

            _allowedListener.Initialize(forwardedExchange, forwardedQueue);
            _allowedListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(T), ProcessAllowed));
            #endregion


            #region suppressed stuff, optionally
            string? suppressedExchange = null, suppressedQueue = null!;

            if (receiveSuppressedToo)
            {
                suppressedExchange = $"rcv_suppressed_{typeof(T).GetFriendlyTypeName()}";
                suppressedQueue = $"q_suppressed_{typeof(T).GetFriendlyTypeName()}";
            
                _messageBusSpecifier.DeclareExchange(suppressedExchange, ExchangeTypes.Direct)
                    .SpecifyExchange(suppressedExchange)
                    .DeclareQueue(suppressedQueue, suppressedQueue);

                _suppressedListener.Initialize(suppressedExchange, suppressedQueue);
                _suppressedListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(T), ProcessSuppressed));
            }
            #endregion

        } catch(Exception ex)
        {
            _logger.LogCritical("{trace}", ex.TraceInformation());
        }


    }

    /// <summary>
    /// -ProcessAllowed() is called for each message received from the allowed message bus.
    /// -It passes the message into the function specicified by the client.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="ct"></param>
    /// <param name="ack"></param>
    private void ProcessAllowed(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        try
        {
            var item = msg as T;
            if (item is not null)
                ItemAllowed?.Invoke(this, new ItemAllowedEventArgs<T> { Item = item });
            ack.MessageAcknowledged();
        } catch(Exception ex)
        {
            _logger.LogError("{trace}", ex.TraceInformation());
            ack.MessageRejected();
        }
    }

    /// <summary>
    /// -ProcessSuppressed() is called for each message received from the suppressed message bus.
    /// -It passes the message into the function specicified by the client.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="ct"></param>
    /// <param name="ack"></param>
    private void ProcessSuppressed(object msg, CancellationToken ct, IMessageAcknowledge ack)
    {
        try
        {
            var item = msg as T;
            if (item is not null)
                ItemSuppressed?.Invoke(this, new ItemSuppressedEventArgs<T> { Item = item });
            ack.MessageAcknowledged();
        } catch(Exception ex)
        {
            _logger.LogError("{trace}", ex.TraceInformation());
            ack.MessageRejected();
        }
    }


}
