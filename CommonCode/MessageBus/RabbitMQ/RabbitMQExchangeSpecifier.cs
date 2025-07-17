using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Linq;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IExchangeSpecifier for managing queues on a specific exchange.
/// </summary>
public class RabbitMQExchangeSpecifier : IExchangeSpecifier
{
    private readonly string _exchangeName;
    private readonly ExchangeTypes _exchangeType;
    private readonly Func<IConnection> _connectionFactory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, RabbitMQQueueSpecifier> _queues;
    private bool _disposed;

    public string Name => _exchangeName;
    public ExchangeTypes ExchangeType => _exchangeType;
    public IEnumerable<IQueueSpecifier> Queues => _queues.Values;

    public RabbitMQExchangeSpecifier(
        string exchangeName, 
        ExchangeTypes exchangeType,
        Func<IConnection> connectionFactory,
        RabbitMQOptions options,
        ILogger logger)
    {
        _exchangeName = exchangeName;
        _exchangeType = exchangeType;
        _connectionFactory = connectionFactory;
        _options = options;
        _logger = logger;
        _queues = new ConcurrentDictionary<string, RabbitMQQueueSpecifier>();
    }

    public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
    {
        queueName.Requires().IsNotNullOrEmpty();
        
        // Use first bound route as primary routing key, or default to "#" for fanout
        var routingKey = boundRoutes?.FirstOrDefault() ?? "#";
        routingKey.Requires().IsNotNullOrEmpty();

        if (!_queues.ContainsKey(queueName))
        {
            using var channel = _connectionFactory().CreateModel();
            
            // Declare the queue
            channel.QueueDeclare(
                queue: queueName,
                durable: _options.QueueDurable,
                exclusive: _options.QueueExclusive,
                autoDelete: _options.QueueAutoDelete,
                arguments: _options.QueueArguments);

            // Bind the queue to the exchange
            channel.QueueBind(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: routingKey);

            var queueSpecifier = new RabbitMQQueueSpecifier(
                queueName, 
                _exchangeName, 
                routingKey,
                _connectionFactory,
                _options,
                _logger);
                
            _queues.TryAdd(queueName, queueSpecifier);
            
            _logger.LogInformation(
                "Declared and bound queue: {QueueName} to exchange: {ExchangeName} with routing key: {RoutingKey}", 
                queueName, _exchangeName, routingKey);
        }

        return this;
    }

    public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
    {
        return DeclareQueue(queueName.ToString(), boundRoutes);
    }

    public IExchangeSpecifier DeleteQueue(string queueName)
    {
        queueName.Requires().IsNotNullOrEmpty();

        if (_queues.TryRemove(queueName, out var queueSpecifier))
        {
            queueSpecifier.Dispose();
            
            using var channel = _connectionFactory().CreateModel();
            channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
            
            _logger.LogInformation("Deleted queue: {QueueName}", queueName);
        }

        return this;
    }

    public IExchangeSpecifier DeleteQueue(Enum queueName)
    {
        return DeleteQueue(queueName.ToString());
    }

    public IQueueSpecifier SpecifyQueue(string queueName)
    {
        queueName.Requires().IsNotNullOrEmpty();

        if (_queues.TryGetValue(queueName, out var queueSpecifier))
        {
            return queueSpecifier;
        }

        throw new InvalidOperationException($"Queue '{queueName}' has not been declared. Call DeclareQueue first.");
    }

    public IQueueSpecifier SpecifyQueue(Enum queueName)
    {
        return SpecifyQueue(queueName.ToString());
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var queue in _queues.Values)
        {
            queue.Dispose();
        }
        _queues.Clear();

        _disposed = true;
    }
}