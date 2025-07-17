using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IMessageBusSpecifier.
/// Maps AMQP-style exchanges to Azure Service Bus Topics and Queues.
/// </summary>
public class AzureServiceBusMessageBus : IMessageBusSpecifier
{
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<AzureServiceBusMessageBus> _logger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ConcurrentDictionary<string, AzureServiceBusExchangeSpecifier> _exchanges;
    private bool _disposed;

    public AzureServiceBusMessageBus(IOptions<AzureServiceBusOptions> options, ILogger<AzureServiceBusMessageBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _exchanges = new ConcurrentDictionary<string, AzureServiceBusExchangeSpecifier>();

        // Create Service Bus client
        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            _client = new ServiceBusClient(_options.ConnectionString, new ServiceBusClientOptions
            {
                TransportType = _options.TransportType,
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = _options.RetryMode,
                    MaxRetries = _options.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs),
                    MaxDelay = TimeSpan.FromSeconds(_options.MaxRetryDelaySeconds),
                    TryTimeout = TimeSpan.FromSeconds(_options.TryTimeoutSeconds)
                }
            });

            _adminClient = new ServiceBusAdministrationClient(_options.ConnectionString);
        }
        else if (!string.IsNullOrEmpty(_options.FullyQualifiedNamespace))
        {
            // Use managed identity
            var credential = new Azure.Identity.DefaultAzureCredential();
            _client = new ServiceBusClient(_options.FullyQualifiedNamespace, credential, new ServiceBusClientOptions
            {
                TransportType = _options.TransportType,
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = _options.RetryMode,
                    MaxRetries = _options.MaxRetries,
                    Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs),
                    MaxDelay = TimeSpan.FromSeconds(_options.MaxRetryDelaySeconds),
                    TryTimeout = TimeSpan.FromSeconds(_options.TryTimeoutSeconds)
                }
            });

            _adminClient = new ServiceBusAdministrationClient(_options.FullyQualifiedNamespace, credential);
        }
        else
        {
            throw new InvalidOperationException("Either ConnectionString or FullyQualifiedNamespace must be provided");
        }

        _logger.LogInformation("Azure Service Bus client initialized");
    }

    public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
    {
        exchangeName.Requires().IsNotNullOrEmpty();

        if (!_exchanges.ContainsKey(exchangeName))
        {
            // In Azure Service Bus:
            // - Direct exchange → Queue
            // - Fanout/Topic exchange → Topic
            var useQueue = exchangeType == ExchangeTypes.Direct;

            if (_options.AutoCreateEntities)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (useQueue)
                        {
                            // Create queue for direct exchange
                            var queueOptions = new CreateQueueOptions(exchangeName)
                            {
                                DefaultMessageTimeToLive = TimeSpan.FromSeconds(_options.DefaultMessageTimeToLiveSeconds),
                                LockDuration = TimeSpan.FromSeconds(_options.LockDurationSeconds),
                                MaxDeliveryCount = _options.MaxDeliveryCount,
                                DeadLetteringOnMessageExpiration = _options.EnableDeadLettering,
                                EnablePartitioning = _options.EnablePartitioning,
                                MaxSizeInMegabytes = _options.MaxSizeInMegabytes,
                                RequiresDuplicateDetection = _options.RequiresDuplicateDetection
                            };

                            if (_options.RequiresDuplicateDetection)
                            {
                                queueOptions.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(_options.DuplicateDetectionWindowMinutes);
                            }

                            if (!await _adminClient.QueueExistsAsync(exchangeName))
                            {
                                await _adminClient.CreateQueueAsync(queueOptions);
                                _logger.LogInformation("Created Azure Service Bus queue: {QueueName}", exchangeName);
                            }
                        }
                        else
                        {
                            // Create topic for fanout/topic exchange
                            var topicOptions = new CreateTopicOptions(exchangeName)
                            {
                                DefaultMessageTimeToLive = TimeSpan.FromSeconds(_options.DefaultMessageTimeToLiveSeconds),
                                EnablePartitioning = _options.EnablePartitioning,
                                MaxSizeInMegabytes = _options.MaxSizeInMegabytes,
                                RequiresDuplicateDetection = _options.RequiresDuplicateDetection,
                                SupportOrdering = _options.SupportOrdering
                            };

                            if (_options.RequiresDuplicateDetection)
                            {
                                topicOptions.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(_options.DuplicateDetectionWindowMinutes);
                            }

                            if (!await _adminClient.TopicExistsAsync(exchangeName))
                            {
                                await _adminClient.CreateTopicAsync(topicOptions);
                                _logger.LogInformation("Created Azure Service Bus topic: {TopicName}", exchangeName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create Azure Service Bus entity: {ExchangeName}", exchangeName);
                    }
                }).Wait(_options.EntityCreationTimeoutMs);
            }

            var exchangeSpecifier = new AzureServiceBusExchangeSpecifier(
                exchangeName, 
                exchangeType, 
                _client, 
                _adminClient, 
                _options, 
                _logger);
                
            _exchanges.TryAdd(exchangeName, exchangeSpecifier);
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

            if (_options.AutoCreateEntities)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Try to delete as queue first
                        if (await _adminClient.QueueExistsAsync(exchangeName))
                        {
                            await _adminClient.DeleteQueueAsync(exchangeName);
                            _logger.LogInformation("Deleted Azure Service Bus queue: {QueueName}", exchangeName);
                        }
                        // Then try as topic
                        else if (await _adminClient.TopicExistsAsync(exchangeName))
                        {
                            await _adminClient.DeleteTopicAsync(exchangeName);
                            _logger.LogInformation("Deleted Azure Service Bus topic: {TopicName}", exchangeName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete Azure Service Bus entity: {ExchangeName}", exchangeName);
                    }
                }).Wait(_options.EntityCreationTimeoutMs);
            }
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

        _client?.DisposeAsync().AsTask().Wait();
        _disposed = true;

        _logger.LogInformation("Disposed Azure Service Bus message bus");
    }
}

/// <summary>
/// Configuration options for Azure Service Bus
/// </summary>
public class AzureServiceBusOptions
{
    // Connection settings
    public string? ConnectionString { get; set; }
    public string? FullyQualifiedNamespace { get; set; }
    
    // Client settings
    public ServiceBusTransportType TransportType { get; set; } = ServiceBusTransportType.AmqpTcp;
    public ServiceBusRetryMode RetryMode { get; set; } = ServiceBusRetryMode.Exponential;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int MaxRetryDelaySeconds { get; set; } = 60;
    public int TryTimeoutSeconds { get; set; } = 60;
    
    // Entity settings
    public bool AutoCreateEntities { get; set; } = true;
    public int EntityCreationTimeoutMs { get; set; } = 30000;
    public int DefaultMessageTimeToLiveSeconds { get; set; } = 86400; // 24 hours
    public int LockDurationSeconds { get; set; } = 60;
    public int MaxDeliveryCount { get; set; } = 10;
    public bool EnableDeadLettering { get; set; } = true;
    public bool EnablePartitioning { get; set; } = false;
    public long MaxSizeInMegabytes { get; set; } = 1024;
    public bool RequiresDuplicateDetection { get; set; } = false;
    public int DuplicateDetectionWindowMinutes { get; set; } = 10;
    public bool SupportOrdering { get; set; } = false;
    public int? AutoDeleteOnIdleMinutes { get; set; }
    
    // Message settings
    public int MaxMessageSizeKilobytes { get; set; } = 256;
    public bool EnableBatching { get; set; } = true;
    public int MaxBatchSize { get; set; } = 100;
    
    // Receiver settings
    public ServiceBusReceiveMode ReceiveMode { get; set; } = ServiceBusReceiveMode.PeekLock;
    public int PrefetchCount { get; set; } = 0;
    public int MaxConcurrentCalls { get; set; } = 1;
    public bool AutoCompleteMessages { get; set; } = false;
    public int MaxAutoLockRenewalDurationMinutes { get; set; } = 5;
}