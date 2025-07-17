using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IExchangeSpecifier.
/// Manages subscriptions on topics or direct queue access.
/// </summary>
public class AzureServiceBusExchangeSpecifier : IExchangeSpecifier
{
    private readonly string _exchangeName;
    private readonly ExchangeTypes _exchangeType;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, AzureServiceBusQueueSpecifier> _queues;
    private readonly bool _isQueue; // true if Direct exchange (queue), false if Topic
    private bool _disposed;

    public string Name => _exchangeName;
    public ExchangeTypes ExchangeType => _exchangeType;
    public IEnumerable<IQueueSpecifier> Queues => _queues.Values;

    public AzureServiceBusExchangeSpecifier(
        string exchangeName,
        ExchangeTypes exchangeType,
        ServiceBusClient client,
        ServiceBusAdministrationClient adminClient,
        AzureServiceBusOptions options,
        ILogger logger)
    {
        _exchangeName = exchangeName;
        _exchangeType = exchangeType;
        _client = client;
        _adminClient = adminClient;
        _options = options;
        _logger = logger;
        _queues = new ConcurrentDictionary<string, AzureServiceBusQueueSpecifier>();
        _isQueue = exchangeType == ExchangeTypes.Direct;
    }

    public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
    {
        queueName.Requires().IsNotNullOrEmpty();
        
        // Use first bound route as primary routing key, or default to "#" for fanout
        var routingKey = boundRoutes?.FirstOrDefault() ?? "#";

        if (!_queues.ContainsKey(queueName))
        {
            if (_isQueue)
            {
                // For direct exchange (queue), we don't need subscriptions
                // The queue name should match the exchange name for direct routing
                if (!string.Equals(_exchangeName, queueName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "For direct exchange, queue name '{QueueName}' should match exchange name '{ExchangeName}'", 
                        queueName, _exchangeName);
                }
            }
            else
            {
                // For topic exchange, create a subscription
                if (_options.AutoCreateEntities)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var subscriptionOptions = new CreateSubscriptionOptions(_exchangeName, queueName)
                            {
                                DefaultMessageTimeToLive = TimeSpan.FromSeconds(_options.DefaultMessageTimeToLiveSeconds),
                                LockDuration = TimeSpan.FromSeconds(_options.LockDurationSeconds),
                                MaxDeliveryCount = _options.MaxDeliveryCount,
                                DeadLetteringOnMessageExpiration = _options.EnableDeadLettering,
                                EnableBatchedOperations = _options.EnableBatching,
                                AutoDeleteOnIdle = _options.AutoDeleteOnIdleMinutes.HasValue && _options.AutoDeleteOnIdleMinutes.Value > 0 
                                    ? TimeSpan.FromMinutes(_options.AutoDeleteOnIdleMinutes.Value) 
                                    : TimeSpan.MaxValue
                            };

                            if (!await _adminClient.SubscriptionExistsAsync(_exchangeName, queueName))
                            {
                                // Create subscription
                                await _adminClient.CreateSubscriptionAsync(subscriptionOptions);
                                
                                // Add routing rule based on exchange type
                                if (_exchangeType == ExchangeTypes.Topic)
                                {
                                    // For topic exchanges, create a SQL filter based on routing key pattern
                                    var filter = CreateTopicFilter(routingKey);
                                    var ruleOptions = new CreateRuleOptions
                                    {
                                        Name = "RoutingRule",
                                        Filter = filter
                                    };
                                    
                                    // Remove default rule and add our routing rule
                                    await _adminClient.DeleteRuleAsync(_exchangeName, queueName, "$Default");
                                    await _adminClient.CreateRuleAsync(_exchangeName, queueName, ruleOptions);
                                }
                                // For fanout, keep the default TRUE rule (receives all messages)
                                
                                _logger.LogInformation(
                                    "Created subscription '{SubscriptionName}' on topic '{TopicName}' with routing key '{RoutingKey}'", 
                                    queueName, _exchangeName, routingKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "Failed to create subscription '{SubscriptionName}' on topic '{TopicName}'", 
                                queueName, _exchangeName);
                        }
                    }).Wait(_options.EntityCreationTimeoutMs);
                }
            }

            var queueSpecifier = new AzureServiceBusQueueSpecifier(
                queueName,
                _exchangeName,
                routingKey,
                _isQueue,
                _client,
                _options,
                _logger);

            _queues.TryAdd(queueName, queueSpecifier);
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

            if (!_isQueue && _options.AutoCreateEntities)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (await _adminClient.SubscriptionExistsAsync(_exchangeName, queueName))
                        {
                            await _adminClient.DeleteSubscriptionAsync(_exchangeName, queueName);
                            _logger.LogInformation(
                                "Deleted subscription '{SubscriptionName}' from topic '{TopicName}'", 
                                queueName, _exchangeName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Failed to delete subscription '{SubscriptionName}' from topic '{TopicName}'", 
                            queueName, _exchangeName);
                    }
                }).Wait(_options.EntityCreationTimeoutMs);
            }
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

    private SqlRuleFilter CreateTopicFilter(string routingKey)
    {
        // Convert AMQP-style routing key patterns to SQL filters
        // Examples:
        // "order.*" → "RoutingKey LIKE 'order.%'"
        // "order.#" → "RoutingKey LIKE 'order.%'"
        // "order.created" → "RoutingKey = 'order.created'"
        
        if (routingKey.Contains("*") || routingKey.Contains("#"))
        {
            // Convert wildcards to SQL LIKE pattern
            var sqlPattern = routingKey
                .Replace(".", "\\.")  // Escape dots
                .Replace("*", "%")    // * matches one segment
                .Replace("#", "%");   // # matches multiple segments
                
            return new SqlRuleFilter($"user.RoutingKey LIKE '{sqlPattern}'");
        }
        else
        {
            // Exact match
            return new SqlRuleFilter($"user.RoutingKey = '{routingKey}'");
        }
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