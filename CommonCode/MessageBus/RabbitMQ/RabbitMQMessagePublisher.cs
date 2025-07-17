using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IMessagePublisher for sending messages to exchanges.
/// </summary>
public class RabbitMQMessagePublisher : IMessagePublisher
{
    private readonly Func<IConnection> _connectionFactory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger _logger;
    private IModel? _channel;
    private string? _exchangeName;
    private bool _disposed;

    public RabbitMQMessagePublisher(
        Func<IConnection> connectionFactory,
        RabbitMQOptions options,
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public void Initialize(string exchangeName)
    {
        _exchangeName = exchangeName;
        EnsureChannel();
    }

    private void EnsureChannel()
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _channel?.Dispose();
            _channel = _connectionFactory().CreateModel();
            
            if (_options.PublisherConfirms)
            {
                _channel.ConfirmSelect();
            }
            
            _channel.BasicQos(0, _options.PrefetchCount, false);
        }
    }

    public void Send<T>(T msg, string routeKey)
    {
        if (_exchangeName == null)
        {
            throw new InvalidOperationException("Publisher not initialized. Call Initialize first.");
        }

        EnsureChannel();
        
        var json = JsonConvert.SerializeObject(msg);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = _channel!.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.ContentType = "application/json";
        properties.ContentEncoding = "utf-8";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.MessageId = Guid.NewGuid().ToString();
        
        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routeKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
            
        if (_options.PublisherConfirms)
        {
            _channel.WaitForConfirmsOrDie(_options.PublishTimeout);
        }
        
        _logger.LogDebug(
            "Published message to exchange: {Exchange}, routing key: {RoutingKey}, message type: {MessageType}", 
            _exchangeName, routeKey, typeof(T).Name);
    }

    public void Send<T>(T msg, Enum routeKey)
    {
        Send(msg, routeKey.ToString());
    }

    public async Task SendAsync<T>(T msg, string routeKey)
    {
        await Task.Run(() => Send(msg, routeKey));
    }

    public async Task SendAsync<T>(T msg, Enum routeKey)
    {
        await Task.Run(() => Send(msg, routeKey.ToString()));
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _channel?.Close();
        _channel?.Dispose();
        _disposed = true;
    }
}