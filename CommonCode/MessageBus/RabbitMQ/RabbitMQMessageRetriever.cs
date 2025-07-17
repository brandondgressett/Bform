using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IMessageRetriever for pulling messages from queues on-demand.
/// </summary>
public class RabbitMQMessageRetriever : IMessageRetriever
{
    private readonly string _queueName;
    private readonly Func<IConnection> _connectionFactory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger _logger;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMQMessageRetriever(
        string queueName,
        Func<IConnection> connectionFactory,
        RabbitMQOptions options,
        ILogger logger)
    {
        _queueName = queueName;
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public void Initialize(string exchangeName, string qName)
    {
        // Queue name is already set in constructor, but we'll validate they match
        if (!string.Equals(_queueName, qName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Retriever was created for queue '{_queueName}' but Initialize was called with '{qName}'");
        }
        // Exchange name can be ignored or stored if needed
    }
    
    public async Task<MessageContext<T>?> MaybeGetMessageAsync<T>() where T : class, new()
    {
        // For RabbitMQ, we'll use the synchronous BasicGet in an async wrapper
        var envelope = await Task.Run(() => GetMessage<T>());
        
        if (envelope == null) return null;
        
        // Convert MessageQueueEnvelope to MessageContext
        return new MessageContext<T>(envelope.Message, envelope.MessageAcknowledge);
    }

    public MessageQueueEnvelope<T>? GetMessage<T>()
    {
        EnsureChannel();
        
        var result = _channel!.BasicGet(_queueName, autoAck: false);
        
        if (result == null)
        {
            return null;
        }
        
        try
        {
            var body = result.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonConvert.DeserializeObject<T>(json);
            
            var envelope = new MessageQueueEnvelope<T>
            {
                Headers = result.BasicProperties?.Headers?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
                QueueName = _queueName,
                Message = message,
                MessageContext = new MessageContextInfo
                {
                    MessageId = result.BasicProperties?.MessageId ?? Guid.NewGuid().ToString(),
                    CorrelationId = result.BasicProperties?.CorrelationId,
                    Timestamp = result.BasicProperties?.Timestamp.UnixTime != null 
                        ? DateTimeOffset.FromUnixTimeSeconds(result.BasicProperties.Timestamp.UnixTime).DateTime 
                        : DateTime.UtcNow,
                    DeliveryTag = result.DeliveryTag,
                    Redelivered = result.Redelivered,
                    Exchange = result.Exchange,
                    RoutingKey = result.RoutingKey
                }
            };
            
            // Create acknowledgment wrapper
            envelope.MessageAcknowledge = new RabbitMQMessageAcknowledge(_channel, result.DeliveryTag, _logger);
            
            _logger.LogDebug("Retrieved message from queue: {QueueName}, MessageId: {MessageId}", 
                _queueName, envelope.MessageContext.MessageId);
            
            return envelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message from queue: {QueueName}", _queueName);
            
            // Reject the message and requeue it
            _channel!.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
            
            throw;
        }
    }

    public async Task<MessageQueueEnvelope<T>?> GetMessageAsync<T>()
    {
        return await Task.Run(() => GetMessage<T>());
    }

    public IEnumerable<MessageQueueEnvelope<T>> GetMessages<T>(int maxMessages)
    {
        EnsureChannel();
        
        var messages = new List<MessageQueueEnvelope<T>>();
        
        for (int i = 0; i < maxMessages; i++)
        {
            var message = GetMessage<T>();
            if (message == null)
            {
                break;
            }
            messages.Add(message);
        }
        
        _logger.LogDebug("Retrieved {Count} messages from queue: {QueueName}", messages.Count, _queueName);
        
        return messages;
    }

    public async Task<IEnumerable<MessageQueueEnvelope<T>>> GetMessagesAsync<T>(int maxMessages)
    {
        return await Task.Run(() => GetMessages<T>(maxMessages));
    }

    private void EnsureChannel()
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _channel?.Dispose();
            _channel = _connectionFactory().CreateModel();
            _channel.BasicQos(0, _options.PrefetchCount, false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _channel?.Close();
        _channel?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// RabbitMQ implementation of IMessageAcknowledge for acknowledging messages.
/// </summary>
public class RabbitMQMessageAcknowledge : IMessageAcknowledge
{
    private readonly IModel _channel;
    private readonly ulong _deliveryTag;
    private readonly ILogger _logger;
    private bool _acknowledged;

    public RabbitMQMessageAcknowledge(IModel channel, ulong deliveryTag, ILogger logger)
    {
        _channel = channel;
        _deliveryTag = deliveryTag;
        _logger = logger;
    }

    public void MessageAcknowledged()
    {
        if (_acknowledged)
        {
            _logger.LogWarning("Message already acknowledged: {DeliveryTag}", _deliveryTag);
            return;
        }
        
        _channel.BasicAck(_deliveryTag, multiple: false);
        _acknowledged = true;
        
        _logger.LogDebug("Message acknowledged: {DeliveryTag}", _deliveryTag);
    }

    public void MessageAbandoned()
    {
        if (_acknowledged)
        {
            _logger.LogWarning("Message already acknowledged: {DeliveryTag}", _deliveryTag);
            return;
        }
        
        _channel.BasicNack(_deliveryTag, multiple: false, requeue: true);
        _acknowledged = true;
        
        _logger.LogDebug("Message abandoned (requeued): {DeliveryTag}", _deliveryTag);
    }

    public void MessageRejected()
    {
        if (_acknowledged)
        {
            _logger.LogWarning("Message already acknowledged: {DeliveryTag}", _deliveryTag);
            return;
        }
        
        _channel.BasicNack(_deliveryTag, multiple: false, requeue: false);
        _acknowledged = true;
        
        _logger.LogDebug("Message rejected (not requeued): {DeliveryTag}", _deliveryTag);
    }
    
    // Keep old methods for backward compatibility
    public void Success() => MessageAcknowledged();
    public void Failed(bool requeue = true)
    {
        if (requeue)
            MessageAbandoned();
        else
            MessageRejected();
    }

    public void Dispose()
    {
        if (!_acknowledged)
        {
            // If not explicitly acknowledged, reject and requeue
            Failed(requeue: true);
        }
    }
}