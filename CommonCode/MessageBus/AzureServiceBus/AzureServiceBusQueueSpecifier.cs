using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IQueueSpecifier.
/// Manages a specific queue or subscription.
/// </summary>
public class AzureServiceBusQueueSpecifier : IQueueSpecifier
{
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly bool _isQueue;
    private readonly ServiceBusClient _client;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger _logger;

    public string Name => _queueName;
    public IEnumerable<string> Bindings => new[] { _routingKey };

    public AzureServiceBusQueueSpecifier(
        string queueName,
        string exchangeName,
        string routingKey,
        bool isQueue,
        ServiceBusClient client,
        AzureServiceBusOptions options,
        ILogger logger)
    {
        _queueName = queueName;
        _exchangeName = exchangeName;
        _routingKey = routingKey;
        _isQueue = isQueue;
        _client = client;
        _options = options;
        _logger = logger;
    }

    public IMessagePublisher GetPublisher()
    {
        return new AzureServiceBusMessagePublisher(_client, _options, _logger);
    }

    public IMessageListener GetListener()
    {
        var listener = new AzureServiceBusMessageListener(_client, _options, _logger);
        listener.Initialize(_exchangeName, _queueName);
        return listener;
    }

    public IMessageRetriever GetRetriever()
    {
        var retriever = new AzureServiceBusMessageRetriever(_client, _options, _logger);
        retriever.Initialize(_exchangeName, _queueName);
        return retriever;
    }

    public void Dispose()
    {
        // Nothing to dispose at this level
    }
}