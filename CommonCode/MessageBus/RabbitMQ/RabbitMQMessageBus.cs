using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.MessageBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of IMessageBusSpecifier for creating and managing exchanges.
/// This class manages the RabbitMQ connection and provides exchange specifications.
/// </summary>
public class RabbitMQMessageBus : IMessageBusSpecifier
{
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private readonly ConcurrentDictionary<string, RabbitMQExchangeSpecifier> _exchanges;
    private bool _disposed;

    public RabbitMQMessageBus(IOptions<RabbitMQOptions> options, ILogger<RabbitMQMessageBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _exchanges = new ConcurrentDictionary<string, RabbitMQExchangeSpecifier>();
        
        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            RequestedHeartbeat = TimeSpan.FromSeconds(_options.HeartbeatInterval),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryInterval),
            AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
            TopologyRecoveryEnabled = _options.TopologyRecoveryEnabled
        };

        if (_options.UseSsl)
        {
            ((ConnectionFactory)_connectionFactory).Ssl = new SslOption
            {
                Enabled = true,
                ServerName = _options.HostName,
                AcceptablePolicyErrors = _options.SslAcceptablePolicyErrors
            };
        }
    }

    private IConnection GetConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            lock (this)
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection?.Dispose();
                    _connection = _connectionFactory.CreateConnection($"BFormDomain-{Environment.MachineName}");
                    _logger.LogInformation("Created new RabbitMQ connection");
                }
            }
        }
        return _connection;
    }

    public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
    {
        exchangeName.Requires().IsNotNullOrEmpty();

        if (!_exchanges.ContainsKey(exchangeName))
        {
            using var channel = GetConnection().CreateModel();
            
            var rabbitExchangeType = exchangeType switch
            {
                ExchangeTypes.Direct => ExchangeType.Direct,
                ExchangeTypes.Fanout => ExchangeType.Fanout,
                ExchangeTypes.Topic => ExchangeType.Topic,
                _ => throw new ArgumentException($"Unknown exchange type: {exchangeType}")
            };

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: rabbitExchangeType,
                durable: _options.ExchangeDurable,
                autoDelete: _options.ExchangeAutoDelete,
                arguments: null);

            var exchangeSpecifier = new RabbitMQExchangeSpecifier(exchangeName, exchangeType, GetConnection, _options, _logger);
            _exchanges.TryAdd(exchangeName, exchangeSpecifier);
            
            _logger.LogInformation("Declared RabbitMQ exchange: {ExchangeName} of type {ExchangeType}", exchangeName, exchangeType);
        }

        return this;
    }

    public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
    {
        return DeclareExchange(exchangeName.ToString(), exchangeType);
    }

    public IMessageBusSpecifier DeleteExchange(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();

        if (_exchanges.TryRemove(exchangeName, out var exchangeSpecifier))
        {
            exchangeSpecifier.Dispose();
            
            using var channel = GetConnection().CreateModel();
            channel.ExchangeDelete(exchangeName, ifUnused: false);
            
            _logger.LogInformation("Deleted RabbitMQ exchange: {ExchangeName}", exchangeName);
        }

        return this;
    }

    public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
    {
        return DeleteExchange(exchangeName.ToString());
    }

    public IExchangeSpecifier SpecifyExchange(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();

        if (_exchanges.TryGetValue(exchangeName, out var exchangeSpecifier))
        {
            return exchangeSpecifier;
        }

        throw new InvalidOperationException($"Exchange '{exchangeName}' has not been declared. Call DeclareExchange first.");
    }

    public IExchangeSpecifier SpecifyExchange(Enum exchangeName)
    {
        return SpecifyExchange(exchangeName.ToString());
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var exchange in _exchanges.Values)
        {
            exchange.Dispose();
        }
        _exchanges.Clear();

        _connection?.Dispose();
        _disposed = true;
        
        _logger.LogInformation("Disposed RabbitMQ message bus");
    }
}

/// <summary>
/// Configuration options for RabbitMQ
/// </summary>
public class RabbitMQOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Connection settings
    public int HeartbeatInterval { get; set; } = 60;
    public int NetworkRecoveryInterval { get; set; } = 5;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public bool TopologyRecoveryEnabled { get; set; } = true;
    
    // SSL settings
    public bool UseSsl { get; set; } = false;
    public System.Net.Security.SslPolicyErrors SslAcceptablePolicyErrors { get; set; } = System.Net.Security.SslPolicyErrors.None;
    
    // Exchange settings
    public bool ExchangeDurable { get; set; } = true;
    public bool ExchangeAutoDelete { get; set; } = false;
    
    // Queue settings
    public bool QueueDurable { get; set; } = true;
    public bool QueueExclusive { get; set; } = false;
    public bool QueueAutoDelete { get; set; } = false;
    public Dictionary<string, object>? QueueArguments { get; set; }
    
    // Consumer settings
    public ushort PrefetchCount { get; set; } = 1;
    public bool ConsumerAutoAck { get; set; } = false;
    
    // Publisher settings
    public bool PublisherConfirms { get; set; } = true;
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(30);
}