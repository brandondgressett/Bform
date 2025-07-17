using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IMessageRetriever.
/// Retrieves messages from queues or topic subscriptions.
/// </summary>
public class AzureServiceBusMessageRetriever : IMessageRetriever
{
    private readonly ServiceBusClient _client;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger _logger;
    
    private string? _entityPath; // Queue or Topic name
    private string? _subscriptionName; // Null for queues
    private ServiceBusReceiver? _receiver;
    private bool _disposed;

    public AzureServiceBusMessageRetriever(
        ServiceBusClient client,
        AzureServiceBusOptions options,
        ILogger logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
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

        // Create receiver options
        var receiverOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = _options.ReceiveMode,
            PrefetchCount = _options.PrefetchCount
        };

        // Create receiver for queue or subscription
        if (string.IsNullOrEmpty(_subscriptionName))
        {
            // Queue receiver
            _receiver = _client.CreateReceiver(_entityPath, receiverOptions);
            _logger.LogDebug("Created receiver for queue: {QueueName}", _entityPath);
        }
        else
        {
            // Topic subscription receiver
            _receiver = _client.CreateReceiver(_entityPath, _subscriptionName, receiverOptions);
            _logger.LogDebug("Created receiver for topic: {TopicName}, subscription: {SubscriptionName}", 
                _entityPath, _subscriptionName);
        }
    }

    public async Task<MessageContext<T>?> MaybeGetMessageAsync<T>() where T : class, new()
    {
        if (_receiver == null)
        {
            throw new InvalidOperationException("Retriever not initialized. Call Initialize first.");
        }

        try
        {
            // Receive a single message with timeout
            var message = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
            
            if (message == null)
            {
                _logger.LogDebug("No messages available on entity: {EntityPath}", _entityPath);
                return null;
            }

            // Extract and deserialize message body
            var body = message.Body.ToString();
            T? deserializedMessage;
            
            try
            {
                deserializedMessage = JsonConvert.DeserializeObject<T>(body);
                if (deserializedMessage == null)
                {
                    _logger.LogError("Failed to deserialize message from entity: {EntityPath}", _entityPath);
                    
                    // Abandon the message
                    if (_options.ReceiveMode == ServiceBusReceiveMode.PeekLock)
                    {
                        await _receiver.AbandonMessageAsync(message);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing message from entity: {EntityPath}", _entityPath);
                
                // Abandon the message
                if (_options.ReceiveMode == ServiceBusReceiveMode.PeekLock)
                {
                    await _receiver.AbandonMessageAsync(message);
                }
                return null;
            }

            _logger.LogDebug(
                "Retrieved and deserialized message from entity: {EntityPath}, MessageId: {MessageId}, Type: {MessageType}",
                _entityPath, message.MessageId, typeof(T).Name);

            // Create acknowledge wrapper
            IMessageAcknowledge? acknowledge = null!;
            if (_options.ReceiveMode == ServiceBusReceiveMode.PeekLock)
            {
                acknowledge = new AzureServiceBusReceiverAcknowledge(message, _receiver, _logger);
            }

            return new MessageContext<T>(deserializedMessage, acknowledge);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
        {
            // Timeout is expected when no messages are available
            _logger.LogDebug("Receive timeout on entity: {EntityPath}", _entityPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving typed message from entity: {EntityPath}", _entityPath);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _receiver?.DisposeAsync().AsTask().Wait();
        _disposed = true;
    }
}

/// <summary>
/// Implementation of IMessageAcknowledge for Azure Service Bus receiver.
/// </summary>
internal class AzureServiceBusReceiverAcknowledge : IMessageAcknowledge
{
    private readonly ServiceBusReceivedMessage _message;
    private readonly ServiceBusReceiver _receiver;
    private readonly ILogger _logger;
    private bool _handled;

    public AzureServiceBusReceiverAcknowledge(
        ServiceBusReceivedMessage message,
        ServiceBusReceiver receiver,
        ILogger logger)
    {
        _message = message;
        _receiver = receiver;
        _logger = logger;
    }

    public void MessageAcknowledged()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            _receiver.CompleteMessageAsync(_message).Wait();
            _logger.LogDebug("Message acknowledged: {MessageId}", _message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message: {MessageId}", _message.MessageId);
        }
    }

    public void MessageAbandoned()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            _receiver.AbandonMessageAsync(_message).Wait();
            _logger.LogDebug("Message abandoned: {MessageId}", _message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning message: {MessageId}", _message.MessageId);
        }
    }

    public void MessageRejected()
    {
        if (_handled) return;
        _handled = true;

        try
        {
            // In Azure Service Bus, we dead letter rejected messages
            _receiver.DeadLetterMessageAsync(_message, "Rejected", "Message was rejected by handler").Wait();
            _logger.LogDebug("Message rejected and dead-lettered: {MessageId}", _message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting message: {MessageId}", _message.MessageId);
        }
    }
}