# Azure Service Bus Message Bus Implementation

This directory contains the Azure Service Bus implementation of the BFormDomain message bus interfaces.

## Overview

The Azure Service Bus implementation maps AMQP-style messaging concepts to Azure Service Bus entities:

- **Direct Exchange** → Azure Service Bus Queue
- **Fanout Exchange** → Azure Service Bus Topic (with subscriptions)
- **Topic Exchange** → Azure Service Bus Topic (with filtered subscriptions)

## Components

### AzureServiceBusMessageBus
The main message bus implementation that manages exchanges (queues/topics) and provides factory methods for creating exchange specifiers.

### AzureServiceBusExchangeSpecifier
Manages a specific exchange (queue or topic) and provides factory methods for creating queue specifiers (subscriptions).

### AzureServiceBusQueueSpecifier
Represents a specific queue or subscription and provides factory methods for creating publishers, listeners, and retrievers.

### AzureServiceBusMessagePublisher
Publishes messages to queues or topics with support for:
- Single message publishing
- Batch message publishing
- Routing key support (using Subject and ApplicationProperties)
- Message TTL and other properties

### AzureServiceBusMessageListener
Listens for messages from queues or subscriptions with:
- Event-based message processing
- Automatic or manual message acknowledgment
- Concurrent message processing
- Error handling and retry logic

### AzureServiceBusMessageRetriever
Retrieves messages from queues or subscriptions with:
- Pull-based message retrieval
- Single or batch message retrieval
- Typed message deserialization
- Manual message acknowledgment

## Configuration

### Connection Options

You can connect to Azure Service Bus using either:

1. **Connection String** (for development/testing):
```json
{
  "MessageBus": {
    "Type": "AzureServiceBus",
    "AzureServiceBus": {
      "ConnectionString": "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key"
    }
  }
}
```

2. **Managed Identity** (for production):
```json
{
  "MessageBus": {
    "Type": "AzureServiceBus",
    "AzureServiceBus": {
      "FullyQualifiedNamespace": "namespace.servicebus.windows.net"
    }
  }
}
```

### Key Configuration Options

- **AutoCreateEntities**: Automatically create queues/topics/subscriptions if they don't exist
- **ReceiveMode**: `PeekLock` (default) or `ReceiveAndDelete`
- **EnablePartitioning**: Enable partitioning for higher throughput
- **EnableDeadLettering**: Enable dead letter queue for failed messages
- **MaxConcurrentCalls**: Number of concurrent message handlers
- **PrefetchCount**: Number of messages to prefetch for performance

## Usage Examples

### Publishing Messages

```csharp
// Direct to queue
var messageBus = serviceProvider.GetService<IMessageBusSpecifier>();
messageBus.DeclareExchange("order-queue", ExchangeTypes.Direct)
    .DeclareQueue("order-queue", "order")
    .SpecifyQueue("order-queue")
    .GetPublisher()
    .Send(new OrderCreatedEvent { OrderId = 123 }, "order.created");

// Fanout to topic
messageBus.DeclareExchange("notifications", ExchangeTypes.Fanout)
    .DeclareQueue("email-notifications", "#")
    .DeclareQueue("sms-notifications", "#")
    .SpecifyQueue("email-notifications")
    .GetPublisher()
    .Send(new NotificationEvent { Message = "Hello" }, "notification");
```

### Consuming Messages

```csharp
// Listen to queue
var listener = messageBus
    .SpecifyExchange("order-queue")
    .SpecifyQueue("order-queue")
    .GetListener();

listener.SetOnMessage(message =>
{
    var order = JsonConvert.DeserializeObject<OrderCreatedEvent>(message);
    // Process order
    return true; // Return true to acknowledge, false to reject
});

listener.Start();
```

### Retrieving Messages (Pull-based)

```csharp
// Retrieve from subscription
var retriever = messageBus
    .SpecifyExchange("notifications")
    .SpecifyQueue("email-notifications")
    .GetRetriever();

var processedCount = retriever.ReceiveBatch(message =>
{
    // Process message
    return true;
}, maxMessages: 10);
```

## Routing and Filtering

For topic exchanges, routing keys are converted to SQL filters:

- `order.*` → `user.RoutingKey LIKE 'order.%'`
- `order.#` → `user.RoutingKey LIKE 'order.%'`
- `order.created` → `user.RoutingKey = 'order.created'`

The routing key is stored in both the message Subject and ApplicationProperties["RoutingKey"] for compatibility.

## Performance Considerations

1. **Connection Pooling**: The ServiceBusClient is shared across all components
2. **Prefetching**: Enable prefetching for better throughput
3. **Batching**: Use batch operations for bulk message processing
4. **Partitioning**: Enable for high-throughput scenarios
5. **Sessions**: Not currently supported but can be added if needed

## Error Handling

The implementation includes:
- Automatic retry with exponential backoff
- Dead letter queue support
- Comprehensive logging
- Graceful degradation when entities don't exist

## Migration from RabbitMQ

The implementation maintains API compatibility with the RabbitMQ implementation:
- Same interface methods
- Same exchange/queue concepts
- Similar routing key behavior
- Compatible message serialization (JSON)

Key differences:
- Queues map to Azure Service Bus Queues
- Exchanges map to Topics (except Direct which maps to Queues)
- Routing keys use SQL filters instead of AMQP patterns
- No support for headers exchange type