# RabbitMQ Message Bus Implementation

This directory contains the RabbitMQ implementation of the BFormDomain message bus interfaces.

## Features

- Full implementation of AMQP-style messaging interfaces
- Support for Direct, Fanout, and Topic exchanges
- Automatic connection recovery and topology recovery
- Publisher confirms for reliable message delivery
- Message acknowledgment with manual ACK/NACK support
- SSL/TLS support for secure connections
- Configurable connection pooling and heartbeat settings
- Batch message retrieval support
- Dead letter queue support through queue arguments

## Configuration

To use RabbitMQ instead of the in-memory message bus, set the following in your `appsettings.json`:

```json
{
  "MessageBus": {
    "Type": "RabbitMQ",
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/",
      "PrefetchCount": 1,
      "ConsumerAutoAck": false,
      "PublisherConfirms": true
    }
  }
}
```

## Usage Example

```csharp
// In Startup.cs or Program.cs
services.AddBFormDomain(configuration);

// The message bus is automatically configured based on appsettings.json
// You can now inject IMessageBusSpecifier into your services

public class MyService
{
    private readonly IMessageBusSpecifier _messageBus;
    
    public MyService(IMessageBusSpecifier messageBus)
    {
        _messageBus = messageBus;
    }
    
    public async Task SendMessageAsync()
    {
        // Declare an exchange
        _messageBus.DeclareExchange("myExchange", ExchangeTypes.Topic);
        
        // Declare a queue and bind it to the exchange
        var exchange = _messageBus.SpecifyExchange("myExchange");
        exchange.DeclareQueue("myQueue", "routing.key.*");
        
        // Get a publisher
        var queue = exchange.SpecifyQueue("myQueue");
        var publisher = queue.GetPublisher();
        publisher.Initialize("myExchange");
        
        // Send a message
        var message = new MyMessage { Id = 1, Content = "Hello RabbitMQ!" };
        await publisher.SendAsync(message, "routing.key.test");
    }
    
    public void StartListening()
    {
        var exchange = _messageBus.SpecifyExchange("myExchange");
        var queue = exchange.SpecifyQueue("myQueue");
        var listener = queue.GetListener();
        
        // Start listening with typed callback
        listener.BeginReceive<MyMessage>(envelope =>
        {
            Console.WriteLine($"Received: {envelope.Message.Content}");
            
            // Acknowledge the message
            envelope.MessageAcknowledge?.Success();
        });
    }
}
```

## Connection Management

The RabbitMQ implementation automatically manages connections with the following features:

- **Connection Pooling**: A single connection is shared across all channels
- **Automatic Recovery**: Connections are automatically recovered after network failures
- **Heartbeat Monitoring**: Configurable heartbeat interval to detect connection issues
- **Thread Safety**: All operations are thread-safe

## Message Acknowledgment

Messages are not automatically acknowledged by default. You must explicitly acknowledge messages:

```csharp
// Success - removes message from queue
envelope.MessageAcknowledge?.Success();

// Failed with requeue - message goes back to queue
envelope.MessageAcknowledge?.Failed(requeue: true);

// Failed without requeue - message is discarded
envelope.MessageAcknowledge?.Failed(requeue: false);
```

## Advanced Configuration

### SSL/TLS Support

```json
{
  "MessageBus": {
    "RabbitMQ": {
      "UseSsl": true,
      "SslAcceptablePolicyErrors": "RemoteCertificateNameMismatch"
    }
  }
}
```

### Queue Arguments (TTL, Max Length, DLX)

```json
{
  "MessageBus": {
    "RabbitMQ": {
      "QueueArguments": {
        "x-message-ttl": 3600000,
        "x-max-length": 1000000,
        "x-dead-letter-exchange": "dlx",
        "x-dead-letter-routing-key": "failed"
      }
    }
  }
}
```

## Monitoring and Logging

The implementation includes detailed logging at various levels:

- `Debug`: Message publishing and retrieval operations
- `Information`: Connection establishment, queue/exchange declarations
- `Warning`: Message acknowledgment issues, connection problems
- `Error`: Unhandled exceptions, critical failures

Configure logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "BFormDomain.MessageBus": "Debug",
      "RabbitMQ.Client": "Warning"
    }
  }
}
```

## Performance Considerations

1. **Prefetch Count**: Set `PrefetchCount` based on your consumer's processing speed
2. **Publisher Confirms**: Disable `PublisherConfirms` for better throughput if message loss is acceptable
3. **Batch Processing**: Use `GetMessages<T>(int maxMessages)` for batch retrieval
4. **Connection Reuse**: The implementation reuses connections and channels where possible

## Troubleshooting

### Connection Issues
- Check RabbitMQ server is running and accessible
- Verify credentials and virtual host permissions
- Check firewall rules for port 5672 (or custom port)

### Message Not Received
- Ensure exchange and queue are properly declared
- Verify routing key matches the binding
- Check queue has consumers attached
- Look for dead letter queues if configured

### Performance Issues
- Increase `PrefetchCount` for higher throughput
- Disable `PublisherConfirms` if not needed
- Use batch operations where possible
- Monitor RabbitMQ management UI for bottlenecks