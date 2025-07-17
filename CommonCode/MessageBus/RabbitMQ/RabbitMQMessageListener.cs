using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IMessageListener for consuming messages from queues.
/// </summary>
public class RabbitMQMessageListener : IMessageListener
{
    private readonly string _queueName;
    private readonly Func<IConnection> _connectionFactory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger _logger;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private string? _consumerTag;
    private bool _disposed;
    private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _listeners = new();

    public event EventHandler<MessageEventArgs>? MessageReceived;
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<IEnumerable<object>>? ListenAborted;
#pragma warning restore CS0067
    
    public bool Paused { get; set; }

    public RabbitMQMessageListener(
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

    public void Initialize(string queueName)
    {
        // Queue name is already set in constructor, but we'll validate they match
        if (!string.Equals(_queueName, queueName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Listener was created for queue '{_queueName}' but Initialize was called with '{queueName}'");
        }
    }

    public void BeginReceive()
    {
        if (_consumer != null)
        {
            throw new InvalidOperationException("Listener is already receiving messages");
        }

        EnsureChannel();
        
        _consumer = new EventingBasicConsumer(_channel!);
        _consumer.Received += OnMessageReceived;
        
        _consumerTag = _channel!.BasicConsume(
            queue: _queueName,
            autoAck: _options.ConsumerAutoAck,
            consumer: _consumer);
            
        _logger.LogInformation("Started listening on queue: {QueueName}", _queueName);
    }

    public void BeginReceive<T>(Action<MessageQueueEnvelope<T>> callBack)
    {
        if (_consumer != null)
        {
            throw new InvalidOperationException("Listener is already receiving messages");
        }

        EnsureChannel();
        
        _consumer = new EventingBasicConsumer(_channel!);
        _consumer.Received += (sender, args) =>
        {
            try
            {
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<T>(json);
                
                var envelope = new MessageQueueEnvelope<T>
                {
                    Headers = args.BasicProperties?.Headers?.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value?.ToString() ?? string.Empty) ?? new Dictionary<string, string>(),
                    QueueName = _queueName,
                    Message = message,
                    MessageContext = new MessageContextInfo
                    {
                        MessageId = args.BasicProperties?.MessageId ?? Guid.NewGuid().ToString(),
                        CorrelationId = args.BasicProperties?.CorrelationId,
                        Timestamp = args.BasicProperties?.Timestamp.UnixTime != null 
                            ? DateTimeOffset.FromUnixTimeSeconds(args.BasicProperties.Timestamp.UnixTime).DateTime 
                            : DateTime.UtcNow,
                        DeliveryTag = args.DeliveryTag,
                        Redelivered = args.Redelivered,
                        Exchange = args.Exchange,
                        RoutingKey = args.RoutingKey
                    }
                };
                
                callBack(envelope);
                
                if (!_options.ConsumerAutoAck)
                {
                    _channel!.BasicAck(args.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue: {QueueName}", _queueName);
                
                if (!_options.ConsumerAutoAck)
                {
                    _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                }
            }
        };
        
        _consumerTag = _channel!.BasicConsume(
            queue: _queueName,
            autoAck: _options.ConsumerAutoAck,
            consumer: _consumer);
            
        _logger.LogInformation("Started listening on queue: {QueueName} with typed callback", _queueName);
    }

    public void StopReceive()
    {
        if (_consumerTag != null && _channel != null && _channel.IsOpen)
        {
            _channel.BasicCancel(_consumerTag);
            _consumerTag = null!;
        }
        
        if (_consumer != null)
        {
            _consumer.Received -= OnMessageReceived;
            _consumer = null!;
        }
        
        _logger.LogInformation("Stopped listening on queue: {QueueName}", _queueName);
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

    private void OnMessageReceived(object? sender, BasicDeliverEventArgs args)
    {
        try
        {
            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            
            var messageArgs = new MessageEventArgs
            {
                MessageContent = json,
                MessageId = args.BasicProperties?.MessageId ?? Guid.NewGuid().ToString(),
                DeliveryTag = args.DeliveryTag,
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                Redelivered = args.Redelivered
            };
            
            MessageReceived?.Invoke(this, messageArgs);
            
            if (!_options.ConsumerAutoAck)
            {
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message received event handler");
            
            if (!_options.ConsumerAutoAck)
            {
                _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    public void Initialize(string exchangeName, string qName)
    {
        // Queue name is already set in constructor, but we'll validate they match
        if (!string.Equals(_queueName, qName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Listener was created for queue '{_queueName}' but Initialize was called with '{qName}'");
        }
        // Exchange name can be ignored or stored if needed
    }

    public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
    {
        foreach (var kvp in listener)
        {
            _listeners[kvp.Key] = kvp.Value;
        }
        
        // Start receiving messages if not already started
        if (_consumer == null)
        {
            BeginReceive();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        StopReceive();
        
        _channel?.Close();
        _channel?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Event args for message received events
/// </summary>
public class MessageEventArgs : EventArgs
{
    public string MessageContent { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public ulong DeliveryTag { get; set; }
    public string RoutingKey { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public bool Redelivered { get; set; }
}