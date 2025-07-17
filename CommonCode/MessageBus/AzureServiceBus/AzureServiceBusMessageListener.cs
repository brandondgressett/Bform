using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IMessageListener.
/// Consumes messages from queues or topic subscriptions.
/// </summary>
public class AzureServiceBusMessageListener : IMessageListener
{
    private readonly ServiceBusClient _client;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger _logger;
    
    private string? _entityPath; // Queue or Topic name
    private string? _subscriptionName; // Null for queues
    private ServiceBusProcessor? _processor;
    private readonly ConcurrentDictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _handlers;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private volatile bool _paused;

    public bool Paused
    {
        get => _paused;
        set => _paused = value;
    }

    public event EventHandler<IEnumerable<object>>? ListenAborted;

    public AzureServiceBusMessageListener(
        ServiceBusClient client,
        AzureServiceBusOptions options,
        ILogger logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
        _handlers = new ConcurrentDictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();
    }

    public void Initialize(string exchangeName, string qName)
    {
        _entityPath = exchangeName;
        
        // For topics, the qName is the subscription name
        // For queues, we ignore qName as the exchange name is the queue name
        if (!exchangeName.Equals(qName, StringComparison.OrdinalIgnoreCase))
        {
            _subscriptionName = qName;
        }
        
        _logger.LogDebug("Initialized listener for entity: {EntityPath}, subscription: {SubscriptionName}", 
            _entityPath, _subscriptionName ?? "N/A");
    }

    public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
    {
        if (_entityPath == null)
        {
            throw new InvalidOperationException("Listener not initialized. Call Initialize first.");
        }

        // Stop existing processor if any
        StopProcessor();
        
        // Clear and add new handlers
        _handlers.Clear();
        foreach (var handler in listener)
        {
            _handlers[handler.Key] = handler.Value;
        }

        if (!_handlers.Any())
        {
            _logger.LogWarning("No message handlers registered");
            return;
        }

        _cts = new CancellationTokenSource();
        
        // Create processor options
        var processorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false, // We'll manually complete based on handler result
            MaxConcurrentCalls = _options.MaxConcurrentCalls,
            PrefetchCount = _options.PrefetchCount,
            ReceiveMode = _options.ReceiveMode,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_options.MaxAutoLockRenewalDurationMinutes)
        };

        // Create processor for queue or subscription
        if (string.IsNullOrEmpty(_subscriptionName))
        {
            // Queue processor
            _processor = _client.CreateProcessor(_entityPath, processorOptions);
            _logger.LogDebug("Created processor for queue: {QueueName}", _entityPath);
        }
        else
        {
            // Topic subscription processor
            _processor = _client.CreateProcessor(_entityPath, _subscriptionName, processorOptions);
            _logger.LogDebug("Created processor for topic: {TopicName}, subscription: {SubscriptionName}", 
                _entityPath, _subscriptionName);
        }

        // Register handlers
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        // Start processing
        _processor.StartProcessingAsync(_cts.Token).Wait();
        
        _logger.LogInformation("Started listening on entity: {EntityPath}, subscription: {SubscriptionName}", 
            _entityPath, _subscriptionName ?? "N/A");
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        if (_paused)
        {
            // Don't process messages while paused
            return;
        }

        var message = args.Message;
        var cancellationToken = args.CancellationToken;

        try
        {
            // Extract message body
            var body = message.Body.ToString();

            _logger.LogDebug(
                "Processing message from entity: {EntityPath}, MessageId: {MessageId}, Subject: {Subject}",
                _entityPath, message.MessageId, message.Subject);

            // Try to deserialize and find appropriate handler
            var processed = false;
            
            foreach (var handlerPair in _handlers)
            {
                try
                {
                    var messageType = handlerPair.Key;
                    var handler = handlerPair.Value;
                    
                    // Try to deserialize as the expected type
                    var deserializedMessage = JsonConvert.DeserializeObject(body, messageType);
                    if (deserializedMessage != null)
                    {
                        // Create acknowledge wrapper
                        var acknowledge = new AzureServiceBusMessageAcknowledge(args, _logger);
                        
                        // Call the handler
                        handler(deserializedMessage, cancellationToken, acknowledge);
                        processed = true;
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Try next handler type
                    continue;
                }
            }

            if (!processed)
            {
                _logger.LogWarning("No handler found for message from entity: {EntityPath}, MessageId: {MessageId}", 
                    _entityPath, message.MessageId);
                
                // Abandon unhandled messages
                await args.AbandonMessageAsync(message, null, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from entity: {EntityPath}", _entityPath);
            
            // Abandon the message on error
            try
            {
                await args.AbandonMessageAsync(message, null, cancellationToken);
            }
            catch (Exception abandonEx)
            {
                _logger.LogError(abandonEx, "Error abandoning message from entity: {EntityPath}", _entityPath);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, 
            "Error in message processor for entity: {EntityPath}, Source: {ErrorSource}, Namespace: {FullyQualifiedNamespace}",
            _entityPath, args.ErrorSource, args.FullyQualifiedNamespace);

        if (args.Exception is ServiceBusException sbEx && sbEx.IsTransient)
        {
            _logger.LogInformation("Transient error occurred, processor will retry");
        }
        else
        {
            // For non-transient errors, trigger the abort event
            OnListenAborted();
        }

        return Task.CompletedTask;
    }

    private void OnListenAborted()
    {
        // Azure Service Bus doesn't provide a way to get pending messages
        // So we'll just notify with an empty collection
        ListenAborted?.Invoke(this, Enumerable.Empty<object>());
    }

    private void StopProcessor()
    {
        if (_processor != null)
        {
            try
            {
                _processor.StopProcessingAsync().Wait();
                _processor.DisposeAsync().AsTask().Wait();
                _processor = null!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping processor for entity: {EntityPath}", _entityPath);
            }
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null!;
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopProcessor();
        _handlers.Clear();
        _disposed = true;
    }
}

/// <summary>
/// Implementation of IMessageAcknowledge for Azure Service Bus messages.
/// </summary>
internal class AzureServiceBusMessageAcknowledge : IMessageAcknowledge
{
    private readonly ProcessMessageEventArgs _args;
    private readonly ILogger _logger;
    private bool _handled;

    public AzureServiceBusMessageAcknowledge(ProcessMessageEventArgs args, ILogger logger)
    {
        _args = args;
        _logger = logger;
    }

    public void MessageAcknowledged()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            _args.CompleteMessageAsync(_args.Message).Wait();
            _logger.LogDebug("Message acknowledged: {MessageId}", _args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message: {MessageId}", _args.Message.MessageId);
        }
    }

    public void MessageAbandoned()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            _args.AbandonMessageAsync(_args.Message).Wait();
            _logger.LogDebug("Message abandoned: {MessageId}", _args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning message: {MessageId}", _args.Message.MessageId);
        }
    }

    public void MessageRejected()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            // In Azure Service Bus, we dead letter rejected messages
            _args.DeadLetterMessageAsync(_args.Message, "Rejected", "Message was rejected by handler").Wait();
            _logger.LogDebug("Message rejected and dead-lettered: {MessageId}", _args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting message: {MessageId}", _args.Message.MessageId);
        }
    }
}