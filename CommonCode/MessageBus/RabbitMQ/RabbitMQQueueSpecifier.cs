using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IQueueSpecifier for managing a specific queue.
/// </summary>
public class RabbitMQQueueSpecifier : IQueueSpecifier
{
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly Func<IConnection> _connectionFactory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger _logger;

    public string Name => _queueName;
    
    public IEnumerable<string> Bindings => new[] { _routingKey };

    public RabbitMQQueueSpecifier(
        string queueName,
        string exchangeName,
        string routingKey,
        Func<IConnection> connectionFactory,
        RabbitMQOptions options,
        ILogger logger)
    {
        _queueName = queueName;
        _exchangeName = exchangeName;
        _routingKey = routingKey;
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
    }

    public IMessagePublisher GetPublisher()
    {
        return new RabbitMQMessagePublisher(_connectionFactory, _options, _logger);
    }

    public IMessageListener GetListener()
    {
        return new RabbitMQMessageListener(_queueName, _connectionFactory, _options, _logger);
    }

    public IMessageRetriever GetRetriever()
    {
        return new RabbitMQMessageRetriever(_queueName, _connectionFactory, _options, _logger);
    }

    public void Dispose()
    {
        // Nothing to dispose at this level
    }
}