using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace BFormDomain.MessageBus.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of IMessagePublisher.
/// Sends messages to queues or topics.
/// </summary>
public class AzureServiceBusMessagePublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger _logger;
    private ServiceBusSender? _sender;
    private string? _entityPath;
    private bool _disposed;

    public AzureServiceBusMessagePublisher(
        ServiceBusClient client,
        AzureServiceBusOptions options,
        ILogger logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public void Initialize(string exchangeName)
    {
        _entityPath = exchangeName;
        _sender?.DisposeAsync().AsTask().Wait();
        _sender = _client.CreateSender(exchangeName);
        
        _logger.LogDebug("Initialized publisher for entity: {EntityPath}", exchangeName);
    }

    public void Send<T>(T msg, string routeKey)
    {
        if (_sender == null || _entityPath == null)
        {
            throw new InvalidOperationException("Publisher not initialized. Call Initialize first.");
        }

        SendAsync(msg, routeKey).Wait();
    }

    public void Send<T>(T msg, Enum routeKey)
    {
        Send(msg, routeKey.ToString());
    }

    public async Task SendAsync<T>(T msg, string routeKey)
    {
        if (_sender == null || _entityPath == null)
        {
            throw new InvalidOperationException("Publisher not initialized. Call Initialize first.");
        }

        try
        {
            var json = JsonConvert.SerializeObject(msg);
            var body = Encoding.UTF8.GetBytes(json);

            var message = new ServiceBusMessage(body)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = routeKey // Subject is used for topic subscription filtering
            };

            // Add routing key as custom property for SQL filter support
            message.ApplicationProperties["RoutingKey"] = routeKey;

            // Set additional properties
            if (_options.DefaultMessageTimeToLiveSeconds > 0)
            {
                message.TimeToLive = TimeSpan.FromSeconds(_options.DefaultMessageTimeToLiveSeconds);
            }

            // Send single message or batch based on configuration
            if (_options.EnableBatching && false) // Disable batching for single message sends
            {
                // Batching is handled separately in production scenarios
                await _sender.SendMessageAsync(message);
            }
            else
            {
                await _sender.SendMessageAsync(message);
            }

            _logger.LogDebug(
                "Published message to entity: {EntityPath}, routing key: {RoutingKey}, message type: {MessageType}",
                _entityPath, routeKey, typeof(T).Name);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageSizeExceeded)
        {
            _logger.LogError(ex, "Message size exceeded limit for entity: {EntityPath}", _entityPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to entity: {EntityPath}", _entityPath);
            throw;
        }
    }

    public async Task SendAsync<T>(T msg, Enum routeKey)
    {
        await SendAsync(msg, routeKey.ToString());
    }

    public async Task SendBatchAsync<T>(IEnumerable<T> messages, string routeKey)
    {
        if (_sender == null || _entityPath == null)
        {
            throw new InvalidOperationException("Publisher not initialized. Call Initialize first.");
        }

        try
        {
            // Create a batch
            using var messageBatch = await _sender.CreateMessageBatchAsync();

            foreach (var msg in messages)
            {
                var json = JsonConvert.SerializeObject(msg);
                var body = Encoding.UTF8.GetBytes(json);

                var message = new ServiceBusMessage(body)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Subject = routeKey
                };

                message.ApplicationProperties["RoutingKey"] = routeKey;

                if (_options.DefaultMessageTimeToLiveSeconds > 0)
                {
                    message.TimeToLive = TimeSpan.FromSeconds(_options.DefaultMessageTimeToLiveSeconds);
                }

                // Try to add the message to the batch
                if (!messageBatch.TryAddMessage(message))
                {
                    // If the batch is full, send it and create a new one
                    await _sender.SendMessagesAsync(messageBatch);
                    
                    // Create new batch and add the current message
                    using var newBatch = await _sender.CreateMessageBatchAsync();
                    if (!newBatch.TryAddMessage(message))
                    {
                        // Single message is too large
                        _logger.LogError("Message too large to fit in batch for entity: {EntityPath}", _entityPath);
                        throw new InvalidOperationException("Message too large for batch");
                    }
                }
            }

            // Send any remaining messages
            if (messageBatch.Count > 0)
            {
                await _sender.SendMessagesAsync(messageBatch);
            }

            _logger.LogDebug(
                "Published batch of {Count} messages to entity: {EntityPath}, routing key: {RoutingKey}",
                messageBatch.Count, _entityPath, routeKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch to entity: {EntityPath}", _entityPath);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _sender?.DisposeAsync().AsTask().Wait();
        _disposed = true;
    }
}