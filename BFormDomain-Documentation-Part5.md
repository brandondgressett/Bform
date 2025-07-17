# BFormDomain Comprehensive Documentation - Part 5

## Repository Infrastructure

The Repository Infrastructure provides a comprehensive data access layer with MongoDB as the primary persistence mechanism. It supports transactions, optimistic concurrency control, query optimization, and a clean abstraction over the underlying database technology.

### Core Components

#### IDataModel Interface
Base interface for all persistable entities:

```csharp
public interface IDataModel
{
    Guid Id { get; set; }
    int Version { get; set; }
}
```

#### IRepository Interface
Generic repository pattern for data access:

```csharp
public interface IRepository<T> where T : class, IDataModel
{
    // Basic CRUD operations
    Task CreateAsync(T item);
    Task CreateAsync(ITransactionContext transactionContext, T item);
    ValueTask<(T? item, int version)> GetByIdAsync(Guid id);
    Task<(T? item, int version)> GetByIdAsync(ITransactionContext transactionContext, Guid id);
    Task UpdateAsync((T item, int version) itemAndVersion);
    Task UpdateAsync(ITransactionContext transactionContext, T item);
    Task DeleteByIdAsync(Guid id);
    Task DeleteByIdAsync(ITransactionContext transactionContext, Guid id);
    
    // Query operations
    Task<(List<T> items, Dictionary<Guid, int> versions)> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true,
        int? skip = null,
        int? limit = null);
    
    Task<(T? item, int version)> GetOneAsync(Expression<Func<T, bool>> filter);
    Task<(T? item, int version)> GetOneAsync(
        ITransactionContext transactionContext,
        Expression<Func<T, bool>> filter);
    
    // Bulk operations
    Task CreateManyAsync(IEnumerable<T> items);
    Task<long> DeleteFilterAsync(Expression<Func<T, bool>> filter);
    
    // Atomic operations
    Task<bool> IncrementOneByIdAsync(
        Guid id,
        Expression<Func<T, object>> field,
        double amount);
    Task<bool> IncrementOneByIdAsync(
        ITransactionContext transactionContext,
        Guid id,
        Expression<Func<T, object>> field,
        double amount);
    
    // Transaction support
    Task<ITransactionContext> OpenTransactionAsync();
    
    // Special operations
    Task UpdateIgnoreVersionAsync((T item, int version) itemAndVersion);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
    Task<long> CountAsync(Expression<Func<T, bool>>? filter = null);
}
```

#### MongoRepository Implementation
MongoDB-specific implementation of IRepository:

```csharp
public class MongoRepository<T> : IRepository<T> where T : class, IDataModel
{
    private readonly IMongoCollection<T> _collection;
    private readonly IMongoDatabase _database;
    private readonly IApplicationAlert _alerts;
    
    public MongoRepository(
        IDataEnvironment dataEnvironment,
        IApplicationAlert alerts)
    {
        _alerts = alerts;
        _database = dataEnvironment.GetDatabase();
        _collection = _database.GetCollection<T>(typeof(T).Name);
        
        // Create indexes
        CreateIndexes();
    }
    
    private void CreateIndexes()
    {
        // Version index for optimistic concurrency
        var versionIndex = Builders<T>.IndexKeys.Ascending(x => x.Version);
        _collection.Indexes.CreateOne(new CreateIndexModel<T>(versionIndex));
        
        // Entity-specific indexes
        if (typeof(IAppEntity).IsAssignableFrom(typeof(T)))
        {
            var entityIndexes = new[]
            {
                Builders<T>.IndexKeys.Ascending("EntityType"),
                Builders<T>.IndexKeys.Ascending("Template"),
                Builders<T>.IndexKeys.Ascending("HostWorkSet"),
                Builders<T>.IndexKeys.Ascending("HostWorkItem"),
                Builders<T>.IndexKeys.Ascending("CreatedDate"),
                Builders<T>.IndexKeys.Ascending("UpdatedDate")
            };
            
            foreach (var index in entityIndexes)
            {
                _collection.Indexes.CreateOne(new CreateIndexModel<T>(index));
            }
        }
    }
    
    public async Task CreateAsync(T item)
    {
        item.Version = 1;
        await _collection.InsertOneAsync(item);
    }
    
    public async Task UpdateAsync((T item, int version) itemAndVersion)
    {
        var (item, expectedVersion) = itemAndVersion;
        var newVersion = expectedVersion + 1;
        
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(x => x.Id, item.Id),
            Builders<T>.Filter.Eq(x => x.Version, expectedVersion)
        );
        
        item.Version = newVersion;
        
        var result = await _collection.ReplaceOneAsync(filter, item);
        
        if (result.ModifiedCount == 0)
        {
            throw new ConcurrencyException(
                $"Optimistic concurrency violation for {typeof(T).Name} with Id {item.Id}");
        }
    }
}
```

#### ITransactionContext Interface
Transaction management abstraction:

```csharp
public interface ITransactionContext : IDisposable
{
    Task CommitAsync();
    Task AbortAsync();
    bool IsActive { get; }
    object UnderlyingTransaction { get; }
}
```

#### MongoTransactionContext Implementation
MongoDB transaction context:

```csharp
public class MongoTransactionContext : ITransactionContext
{
    private readonly IClientSessionHandle _session;
    private bool _isCommitted;
    private bool _isAborted;
    
    public MongoTransactionContext(IClientSessionHandle session)
    {
        _session = session;
        _session.StartTransaction();
    }
    
    public bool IsActive => !_isCommitted && !_isAborted;
    
    public object UnderlyingTransaction => _session;
    
    public async Task CommitAsync()
    {
        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");
            
        await _session.CommitTransactionAsync();
        _isCommitted = true;
    }
    
    public async Task AbortAsync()
    {
        if (!IsActive)
            return;
            
        await _session.AbortTransactionAsync();
        _isAborted = true;
    }
    
    public void Dispose()
    {
        if (IsActive)
        {
            _session.AbortTransaction();
        }
        _session.Dispose();
    }
}
```

#### IDataEnvironment Interface
Database environment abstraction:

```csharp
public interface IDataEnvironment
{
    IMongoDatabase GetDatabase();
    string GetConnectionString();
    string GetDatabaseName();
    Task<IClientSessionHandle> StartSessionAsync();
}
```

### Repository Usage Examples

#### Basic CRUD Operations

```csharp
public class UserService
{
    private readonly IRepository<ApplicationUser> _userRepo;
    
    public async Task<ApplicationUser> CreateUserAsync(string email, string displayName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        
        await _userRepo.CreateAsync(user);
        return user;
    }
    
    public async Task<ApplicationUser?> GetUserAsync(Guid userId)
    {
        var (user, version) = await _userRepo.GetByIdAsync(userId);
        return user;
    }
    
    public async Task UpdateUserAsync(Guid userId, string newDisplayName)
    {
        var (user, version) = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User {userId} not found");
            
        user.DisplayName = newDisplayName;
        user.UpdatedDate = DateTime.UtcNow;
        
        await _userRepo.UpdateAsync((user, version));
    }
}
```

#### Transaction Management

```csharp
public class OrderService
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<Inventory> _inventoryRepo;
    private readonly IRepository<Payment> _paymentRepo;
    
    public async Task<Order> CreateOrderAsync(OrderRequest request)
    {
        using var transaction = await _orderRepo.OpenTransactionAsync();
        
        try
        {
            // Create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                Items = request.Items,
                TotalAmount = CalculateTotal(request.Items),
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };
            
            await _orderRepo.CreateAsync(transaction, order);
            
            // Update inventory
            foreach (var item in request.Items)
            {
                var (inventory, version) = await _inventoryRepo.GetOneAsync(
                    transaction,
                    inv => inv.ProductId == item.ProductId);
                    
                if (inventory == null || inventory.Quantity < item.Quantity)
                    throw new InsufficientInventoryException(item.ProductId);
                    
                inventory.Quantity -= item.Quantity;
                await _inventoryRepo.UpdateAsync(transaction, inventory);
            }
            
            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };
            
            await _paymentRepo.CreateAsync(transaction, payment);
            
            // Commit transaction
            await transaction.CommitAsync();
            
            return order;
        }
        catch
        {
            await transaction.AbortAsync();
            throw;
        }
    }
}
```

#### Complex Queries

```csharp
public class ReportingService
{
    private readonly IRepository<WorkItem> _workItemRepo;
    
    public async Task<WorkItemStatistics> GetWorkItemStatisticsAsync(
        Guid workSetId,
        DateTime startDate,
        DateTime endDate)
    {
        // Get all work items in date range
        var (workItems, versions) = await _workItemRepo.GetAllAsync(
            filter: w => w.HostWorkSet == workSetId &&
                        w.CreatedDate >= startDate &&
                        w.CreatedDate <= endDate,
            orderBy: w => w.CreatedDate,
            ascending: false);
        
        // Calculate statistics
        var stats = new WorkItemStatistics
        {
            TotalCount = workItems.Count,
            CompletedCount = workItems.Count(w => w.Status == "completed"),
            InProgressCount = workItems.Count(w => w.Status == "in-progress"),
            PendingCount = workItems.Count(w => w.Status == "pending"),
            
            AverageCompletionTime = CalculateAverageCompletionTime(workItems),
            
            ByPriority = workItems
                .GroupBy(w => w.Priority)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            ByAssignee = workItems
                .Where(w => w.UserAssignee.HasValue)
                .GroupBy(w => w.UserAssignee!.Value)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            DailyCreation = workItems
                .GroupBy(w => w.CreatedDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyCount
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };
        
        return stats;
    }
}
```

#### Atomic Operations

```csharp
public class MetricsService
{
    private readonly IRepository<MetricCounter> _counterRepo;
    
    public async Task IncrementCounterAsync(string metricName, double amount = 1)
    {
        var (counter, version) = await _counterRepo.GetOneAsync(
            c => c.Name == metricName);
            
        if (counter == null)
        {
            // Create new counter
            counter = new MetricCounter
            {
                Id = Guid.NewGuid(),
                Name = metricName,
                Value = amount,
                LastUpdated = DateTime.UtcNow
            };
            
            await _counterRepo.CreateAsync(counter);
        }
        else
        {
            // Atomically increment existing counter
            await _counterRepo.IncrementOneByIdAsync(
                counter.Id,
                c => c.Value,
                amount);
        }
    }
}
```

#### Bulk Operations

```csharp
public class DataImportService
{
    private readonly IRepository<ImportedRecord> _recordRepo;
    
    public async Task ImportDataAsync(Stream csvStream)
    {
        var records = new List<ImportedRecord>();
        
        using (var reader = new StreamReader(csvStream))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var csvRecords = csv.GetRecords<CsvRecord>();
            
            foreach (var csvRecord in csvRecords)
            {
                records.Add(new ImportedRecord
                {
                    Id = Guid.NewGuid(),
                    ExternalId = csvRecord.Id,
                    Name = csvRecord.Name,
                    Data = csvRecord.Data,
                    ImportedDate = DateTime.UtcNow
                });
                
                // Batch insert every 1000 records
                if (records.Count >= 1000)
                {
                    await _recordRepo.CreateManyAsync(records);
                    records.Clear();
                }
            }
        }
        
        // Insert remaining records
        if (records.Any())
        {
            await _recordRepo.CreateManyAsync(records);
        }
    }
    
    public async Task CleanupOldRecordsAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var deletedCount = await _recordRepo.DeleteFilterAsync(
            r => r.ImportedDate < cutoffDate);
            
        Console.WriteLine($"Deleted {deletedCount} old records");
    }
}
```

### Custom Repository Extensions

```csharp
public static class RepositoryExtensions
{
    // Pagination helper
    public static async Task<PagedResult<T>> GetPagedAsync<T>(
        this IRepository<T> repository,
        Expression<Func<T, bool>>? filter,
        int page,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null)
        where T : class, IDataModel
    {
        var skip = (page - 1) * pageSize;
        
        var (items, versions) = await repository.GetAllAsync(
            filter: filter,
            orderBy: orderBy,
            skip: skip,
            limit: pageSize);
            
        var totalCount = await repository.CountAsync(filter);
        
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
    
    // Retry with exponential backoff
    public static async Task<T> CreateWithRetryAsync<T>(
        this IRepository<T> repository,
        T item,
        int maxRetries = 3)
        where T : class, IDataModel
    {
        var retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(100);
        
        while (true)
        {
            try
            {
                await repository.CreateAsync(item);
                return item;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                if (retryCount >= maxRetries)
                    throw;
                    
                // Generate new ID and retry
                item.Id = Guid.NewGuid();
                retryCount++;
                
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }
}
```

### Best Practices

1. **Always use transactions for multi-entity operations** - Ensure data consistency
2. **Handle optimistic concurrency** - Retry on version conflicts
3. **Create appropriate indexes** - Optimize query performance
4. **Use atomic operations when possible** - Avoid race conditions
5. **Implement repository caching carefully** - Consider cache invalidation
6. **Log repository operations** - Track performance and errors
7. **Use bulk operations for large datasets** - Improve performance
8. **Implement retry logic** - Handle transient failures
9. **Clean up old data** - Implement data retention policies
10. **Monitor collection sizes** - Plan for data growth

---

## MessageBus Infrastructure

The MessageBus Infrastructure provides a flexible messaging system supporting both in-memory and AMQP (Advanced Message Queuing Protocol) implementations. It enables asynchronous communication between different parts of the application using publish-subscribe and point-to-point messaging patterns.

### Core Components

#### IMessagePublisher Interface
Interface for sending messages to exchanges:

```csharp
public interface IMessagePublisher : IDisposable
{
    void Initialize(string exchangeName);
    
    // Synchronous send
    void Send<T>(T msg, string routeKey);
    void Send<T>(T msg, Enum routeKey);
    
    // Asynchronous send
    Task SendAsync<T>(T msg, string routeKey);
    Task SendAsync<T>(T msg, Enum routeKey);
}
```

#### IMessageListener Interface
Interface for receiving messages from queues:

```csharp
public interface IMessageListener : IDisposable
{
    void Initialize(string exchangeName, string qName);
    
    // Register message handlers by type
    void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener);
    
    bool Paused { get; set; }
    event EventHandler<IEnumerable<object>> ListenAborted;
}
```

#### IMessageBusSpecifier Interface
Factory interface for creating exchanges and queues:

```csharp
public interface IMessageBusSpecifier : IDisposable
{
    // Exchange management
    IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType);
    IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType);
    IMessageBusSpecifier DeleteExchange(string exchangeName);
    IMessageBusSpecifier DeleteExchange(Enum exchangeName);
    
    // Get exchange specifier
    IExchangeSpecifier SpecifyExchange(string exchangeName);
    IExchangeSpecifier SpecifyExchange(Enum exchangeName);
}
```

#### Exchange Types
Supported exchange routing types:

```csharp
public enum ExchangeTypes
{
    Direct,    // Route by exact match on routing key
    Topic,     // Route by pattern match on routing key
    Headers,   // Route by message header attributes
    Fanout     // Route to all bound queues
}
```

#### Message Context
Metadata associated with messages:

```csharp
public class MessageContext
{
    public string MessageId { get; set; }
    public string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ContentType { get; set; }
    public Dictionary<string, object> Headers { get; set; }
    public int Priority { get; set; }
    public TimeSpan? Expiration { get; set; }
}
```

### In-Memory Implementation

#### MemMessageBus
In-memory message bus implementation:

```csharp
public class MemMessageBus : IMessageBusSpecifier
{
    private static readonly ConcurrentDictionary<string, MemExchange> Exchanges = new();
    
    public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
    {
        exchangeName.Requires().IsNotNullOrEmpty();
        
        if (!Exchanges.ContainsKey(exchangeName))
        {
            Exchanges.TryAdd(exchangeName,
                new MemExchange(new MessageExchangeDeclaration
                {
                    Name = exchangeName,
                    Type = exchangeType
                }));
        }
        return this;
    }
    
    public IExchangeSpecifier SpecifyExchange(string exchangeName)
    {
        exchangeName.Requires().IsNotNullOrEmpty();
        bool found = Exchanges.TryGetValue(exchangeName, out MemExchange? me);
        found.Guarantees().IsTrue();
        return me!;
    }
}
```

#### MemMessagePublisher
In-memory message publisher:

```csharp
public class MemMessagePublisher : IMessagePublisher
{
    private MemExchange _exchange;
    private readonly IMessageBusSpecifier _messageBus;
    
    public MemMessagePublisher(IMessageBusSpecifier messageBus)
    {
        _messageBus = messageBus;
    }
    
    public void Initialize(string exchangeName)
    {
        _exchange = (MemExchange)_messageBus.SpecifyExchange(exchangeName);
    }
    
    public void Send<T>(T msg, string routeKey)
    {
        var envelope = new LightMessageQueueEnvelope
        {
            Id = Guid.NewGuid(),
            Message = JsonConvert.SerializeObject(msg),
            MessageType = typeof(T).AssemblyQualifiedName,
            RouteKey = routeKey,
            Timestamp = DateTime.UtcNow
        };
        
        _exchange.Route(envelope);
    }
    
    public async Task SendAsync<T>(T msg, string routeKey)
    {
        await Task.Run(() => Send(msg, routeKey));
    }
}
```

#### MemMessageListener
In-memory message listener with type-based dispatch:

```csharp
public class MemMessageListener : IMessageListener
{
    private readonly IMessageBusSpecifier _messageBus;
    private readonly CancellationTokenSource _cancellationSource = new();
    private Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _handlers;
    private IMemQueueAccess _queue;
    
    public bool Paused { get; set; }
    public event EventHandler<IEnumerable<object>> ListenAborted;
    
    public void Initialize(string exchangeName, string qName)
    {
        var exchange = _messageBus.SpecifyExchange(exchangeName);
        _queue = (IMemQueueAccess)exchange.SpecifyQueue(qName);
    }
    
    public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listeners)
    {
        _handlers = listeners.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        Task.Run(async () =>
        {
            while (!_cancellationSource.Token.IsCancellationRequested)
            {
                if (Paused)
                {
                    await Task.Delay(100);
                    continue;
                }
                
                var envelope = _queue.Dequeue();
                if (envelope != null)
                {
                    ProcessMessage(envelope);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        });
    }
    
    private void ProcessMessage(LightMessageQueueEnvelope envelope)
    {
        try
        {
            var messageType = Type.GetType(envelope.MessageType);
            var message = JsonConvert.DeserializeObject(envelope.Message, messageType);
            
            if (_handlers.TryGetValue(messageType, out var handler))
            {
                var ack = new MemQueueAcknowledge(envelope.Id);
                handler(message, _cancellationSource.Token, ack);
            }
        }
        catch (Exception ex)
        {
            ListenAborted?.Invoke(this, new[] { ex });
        }
    }
}
```

### MessageBus Usage Examples

#### Event-Driven Communication

```csharp
public class OrderProcessingService
{
    private readonly IMessagePublisher _publisher;
    private readonly IMessageListener _listener;
    
    public OrderProcessingService(IMessageBusSpecifier messageBus)
    {
        // Setup publisher
        messageBus.DeclareExchange("order-events", ExchangeTypes.Topic);
        _publisher = new MemMessagePublisher(messageBus);
        _publisher.Initialize("order-events");
        
        // Setup listener
        var exchange = messageBus.SpecifyExchange("order-events");
        exchange.DeclareQueue("order-processor")
                .BindQueue("order-processor", "order.*");
        
        _listener = new MemMessageListener(messageBus);
        _listener.Initialize("order-events", "order-processor");
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Publish order created event
        await _publisher.SendAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedDate = DateTime.UtcNow
        }, "order.created");
        
        // Process order...
        
        // Publish order processed event
        await _publisher.SendAsync(new OrderProcessedEvent
        {
            OrderId = order.Id,
            ProcessedDate = DateTime.UtcNow
        }, "order.processed");
    }
    
    public void StartListening()
    {
        _listener.Listen(
            new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(OrderCreatedEvent),
                async (msg, ct, ack) =>
                {
                    var orderEvent = (OrderCreatedEvent)msg;
                    await HandleOrderCreated(orderEvent);
                    ack.Acknowledge();
                }),
            new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(OrderProcessedEvent),
                async (msg, ct, ack) =>
                {
                    var orderEvent = (OrderProcessedEvent)msg;
                    await HandleOrderProcessed(orderEvent);
                    ack.Acknowledge();
                })
        );
    }
}
```

#### Work Queue Pattern

```csharp
public class TaskDistributionService
{
    private readonly IMessageBusSpecifier _messageBus;
    private readonly List<IMessageListener> _workers = new();
    
    public TaskDistributionService(IMessageBusSpecifier messageBus)
    {
        _messageBus = messageBus;
        
        // Create work queue
        _messageBus.DeclareExchange("task-queue", ExchangeTypes.Direct);
        var exchange = _messageBus.SpecifyExchange("task-queue");
        exchange.DeclareQueue("work-items")
                .BindQueue("work-items", "task");
    }
    
    public void StartWorkers(int workerCount)
    {
        for (int i = 0; i < workerCount; i++)
        {
            var worker = new MemMessageListener(_messageBus);
            worker.Initialize("task-queue", "work-items");
            
            var workerId = i;
            worker.Listen(
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                    typeof(WorkTask),
                    async (msg, ct, ack) =>
                    {
                        var task = (WorkTask)msg;
                        Console.WriteLine($"Worker {workerId} processing task {task.Id}");
                        
                        try
                        {
                            await ProcessWorkTask(task);
                            ack.Acknowledge();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Worker {workerId} failed: {ex.Message}");
                            ack.Reject(true); // Requeue
                        }
                    })
            );
            
            _workers.Add(worker);
        }
    }
    
    public async Task DistributeTaskAsync(WorkTask task)
    {
        var publisher = new MemMessagePublisher(_messageBus);
        publisher.Initialize("task-queue");
        await publisher.SendAsync(task, "task");
    }
}
```

#### Topic-Based Routing

```csharp
public class LoggingService
{
    private readonly IMessageBusSpecifier _messageBus;
    
    public LoggingService(IMessageBusSpecifier messageBus)
    {
        _messageBus = messageBus;
        
        // Create topic exchange for logs
        _messageBus.DeclareExchange("logs", ExchangeTypes.Topic);
        var exchange = _messageBus.SpecifyExchange("logs");
        
        // Different queues for different log levels
        exchange.DeclareQueue("error-logs")
                .BindQueue("error-logs", "log.error.*");
                
        exchange.DeclareQueue("warning-logs")
                .BindQueue("warning-logs", "log.warning.*");
                
        exchange.DeclareQueue("all-logs")
                .BindQueue("all-logs", "log.*");
    }
    
    public void SetupErrorHandler()
    {
        var listener = new MemMessageListener(_messageBus);
        listener.Initialize("logs", "error-logs");
        
        listener.Listen(
            new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(LogMessage),
                async (msg, ct, ack) =>
                {
                    var log = (LogMessage)msg;
                    await AlertOpsTeam(log);
                    await SaveToErrorDatabase(log);
                    ack.Acknowledge();
                })
        );
    }
    
    public async Task LogAsync(LogLevel level, string source, string message)
    {
        var publisher = new MemMessagePublisher(_messageBus);
        publisher.Initialize("logs");
        
        var routingKey = $"log.{level.ToString().ToLower()}.{source}";
        
        await publisher.SendAsync(new LogMessage
        {
            Level = level,
            Source = source,
            Message = message,
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName
        }, routingKey);
    }
}
```

#### Request-Reply Pattern

```csharp
public class RpcService
{
    private readonly IMessageBusSpecifier _messageBus;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingRequests = new();
    
    public RpcService(IMessageBusSpecifier messageBus)
    {
        _messageBus = messageBus;
        
        // Setup request/reply exchanges
        _messageBus.DeclareExchange("rpc-requests", ExchangeTypes.Direct);
        _messageBus.DeclareExchange("rpc-replies", ExchangeTypes.Direct);
        
        // Setup reply listener
        SetupReplyListener();
    }
    
    private void SetupReplyListener()
    {
        var exchange = _messageBus.SpecifyExchange("rpc-replies");
        var replyQueue = $"reply-{Guid.NewGuid()}";
        exchange.DeclareQueue(replyQueue)
                .BindQueue(replyQueue, replyQueue);
        
        var listener = new MemMessageListener(_messageBus);
        listener.Initialize("rpc-replies", replyQueue);
        
        listener.Listen(
            new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(
                typeof(RpcReply),
                (msg, ct, ack) =>
                {
                    var reply = (RpcReply)msg;
                    if (_pendingRequests.TryRemove(reply.CorrelationId, out var tcs))
                    {
                        tcs.SetResult(reply.Result);
                    }
                    ack.Acknowledge();
                })
        );
    }
    
    public async Task<TResult> CallAsync<TRequest, TResult>(string method, TRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object>();
        _pendingRequests[correlationId] = tcs;
        
        var publisher = new MemMessagePublisher(_messageBus);
        publisher.Initialize("rpc-requests");
        
        await publisher.SendAsync(new RpcRequest
        {
            Method = method,
            Parameters = request,
            CorrelationId = correlationId,
            ReplyTo = $"reply-{correlationId}"
        }, method);
        
        // Wait for reply with timeout
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(-1, cts.Token));
            
            if (completedTask == tcs.Task)
            {
                return (TResult)await tcs.Task;
            }
            else
            {
                _pendingRequests.TryRemove(correlationId, out _);
                throw new TimeoutException($"RPC call to {method} timed out");
            }
        }
    }
}
```

### Message Patterns and Best Practices

1. **Use appropriate exchange types** - Direct for routing keys, Topic for patterns
2. **Implement proper error handling** - Use acknowledgments and requeuing
3. **Consider message ordering** - Not guaranteed in concurrent scenarios
4. **Implement idempotency** - Handle duplicate messages gracefully
5. **Use correlation IDs** - Track related messages across systems
6. **Set message expiration** - Prevent queue buildup
7. **Monitor queue depths** - Detect processing bottlenecks
8. **Implement circuit breakers** - Handle downstream failures
9. **Use dead letter queues** - Capture failed messages
10. **Test message serialization** - Ensure compatibility

---

## SimilarEntityTracking System

The SimilarEntityTracking System provides sophisticated duplicate suppression and message consolidation capabilities. It helps prevent duplicate processing of similar entities and enables intelligent batching of related items for digest notifications.

### Core Components

#### ITrackSimilar Interface
Base interface for trackable entities:

```csharp
public interface ITrackSimilar
{
    string TargetId { get; set; }
    string ComparisonType { get; }
    long ComparisonHash { get; }
    string ComparisonPropertyString { get; }
}
```

### Duplicate Suppression

#### ICanShutUp Interface
Interface for suppressible entities:

```csharp
public interface ICanShutUp : ITrackSimilar
{
    // How long to suppress duplicates (in minutes)
    int SuppressionTimeMinutes { get; }
}
```

#### DuplicateSuppressionCore
Core engine for duplicate detection:

```csharp
public class DuplicateSuppressionCore
{
    private readonly ISuppressionPersistence _persistence;
    private readonly IApplicationAlert _alerts;
    
    public event EventHandler<ItemSuppressedEventArgs> ItemSuppressed;
    public event EventHandler<ItemAllowedEventArgs> ItemAllowed;
    
    public async Task<bool> ShouldSuppress(ICanShutUp item)
    {
        // Check if similar item was recently processed
        var existingSuppression = await _persistence.GetActiveSuppressionAsync(
            item.ComparisonType,
            item.ComparisonHash);
            
        if (existingSuppression != null && 
            existingSuppression.ExpiresAt > DateTime.UtcNow)
        {
            // Item should be suppressed
            ItemSuppressed?.Invoke(this, new ItemSuppressedEventArgs
            {
                Item = item,
                OriginalItemId = existingSuppression.OriginalItemId,
                SuppressedUntil = existingSuppression.ExpiresAt
            });
            
            return true;
        }
        
        // Record this item to suppress future duplicates
        await _persistence.RecordSuppressionAsync(new SuppressedItem
        {
            Id = Guid.NewGuid(),
            OriginalItemId = item.TargetId,
            ComparisonType = item.ComparisonType,
            ComparisonHash = item.ComparisonHash,
            ComparisonString = item.ComparisonPropertyString,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(item.SuppressionTimeMinutes)
        });
        
        ItemAllowed?.Invoke(this, new ItemAllowedEventArgs { Item = item });
        
        return false;
    }
}
```

#### Suppressible Implementation
Example suppressible notification:

```csharp
public class AlertNotification : ICanShutUp
{
    public string Id { get; set; }
    public string AlertType { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    
    // ICanShutUp implementation
    public int SuppressionTimeMinutes => 60; // Suppress for 1 hour
    
    // ITrackSimilar implementation
    public string TargetId 
    { 
        get => Id; 
        set => Id = value; 
    }
    
    public string ComparisonType => $"Alert:{AlertType}";
    
    public long ComparisonHash => HashCode.Combine(AlertType, Source, Message);
    
    public string ComparisonPropertyString => $"{AlertType}|{Source}|{Message}";
}
```

### Consolidation and Digest

#### IDigestible Interface
Interface for digest-capable entities:

```csharp
public interface IDigestible : ITrackSimilar
{
    DateTime DigestUntil { get; set; }
    DateTime InvocationTime { get; set; }
    string DigestBodyJson { get; set; }
    
    // Limits for digest display
    int HeadLimit { get; set; }
    int TailLimit { get; set; }
    
    // Forward digest to message bus
    string ForwardToExchange { get; set; }
    string ForwardToRoute { get; set; }
}
```

#### ConsolidationCore
Core engine for message consolidation:

```csharp
public class ConsolidationCore : IConsolidationCore
{
    private readonly IRepository<ConsolidatedDigest> _digestRepo;
    
    public async Task<ConsolidatedDigest> ConsolidateAsync(IDigestible item)
    {
        // Find or create digest for this type
        var digest = await FindOrCreateDigestAsync(
            item.ComparisonType,
            item.DigestUntil);
            
        // Add item to digest
        digest.Items.Add(new DigestItem
        {
            ItemId = item.TargetId,
            ComparisonHash = item.ComparisonHash,
            Details = item.DigestBodyJson,
            AddedAt = DateTime.UtcNow
        });
        
        // Check if digest should be sent
        if (ShouldSendDigest(digest))
        {
            await SendDigestAsync(digest);
        }
        else
        {
            await _digestRepo.UpdateAsync(digest);
        }
        
        return digest;
    }
    
    private bool ShouldSendDigest(ConsolidatedDigest digest)
    {
        // Send if time limit reached
        if (DateTime.UtcNow >= digest.DigestUntil)
            return true;
            
        // Send if item count exceeded
        if (digest.Items.Count >= digest.MaxItems)
            return true;
            
        return false;
    }
}
```

### Usage Examples

#### Duplicate Alert Suppression

```csharp
public class AlertProcessingService
{
    private readonly DuplicateSuppressionCore _suppressionCore;
    private readonly INotificationService _notificationService;
    
    public async Task ProcessAlertAsync(SystemAlert alert)
    {
        // Create suppressible alert
        var suppressibleAlert = new AlertNotification
        {
            Id = alert.Id,
            AlertType = alert.Type,
            Source = alert.Source,
            Message = alert.Message,
            Timestamp = alert.Timestamp
        };
        
        // Check if should suppress
        if (await _suppressionCore.ShouldSuppress(suppressibleAlert))
        {
            Console.WriteLine($"Alert suppressed: {alert.Id}");
            return;
        }
        
        // Process alert normally
        await _notificationService.SendAlertAsync(alert);
    }
}

// Example: Multiple identical alerts
var service = new AlertProcessingService(suppressionCore, notificationService);

// First alert - will be sent
await service.ProcessAlertAsync(new SystemAlert
{
    Id = "alert-1",
    Type = "DiskSpace",
    Source = "Server1",
    Message = "Disk space low on drive C:",
    Timestamp = DateTime.UtcNow
});

// Second identical alert within suppression window - will be suppressed
await service.ProcessAlertAsync(new SystemAlert
{
    Id = "alert-2",
    Type = "DiskSpace",
    Source = "Server1",
    Message = "Disk space low on drive C:",
    Timestamp = DateTime.UtcNow.AddMinutes(5)
});
```

#### Email Digest Consolidation

```csharp
public class EmailDigestService
{
    private readonly ConsolidationCore _consolidationCore;
    private readonly IEmailService _emailService;
    
    public async Task QueueForDigestAsync(EmailNotification email)
    {
        var digestible = new DigestibleEmail
        {
            Id = email.Id,
            RecipientEmail = email.To,
            Subject = email.Subject,
            Body = email.Body,
            
            // Digest configuration
            DigestUntil = DateTime.UtcNow.Date.AddDays(1).AddHours(9), // Next day 9 AM
            InvocationTime = DateTime.UtcNow,
            DigestBodyJson = JsonConvert.SerializeObject(new
            {
                email.Subject,
                Preview = email.Body.Substring(0, Math.Min(100, email.Body.Length))
            }),
            
            HeadLimit = 5,  // Show first 5 items
            TailLimit = 3,  // Show last 3 items
            
            ForwardToExchange = "email-digests",
            ForwardToRoute = "daily-digest"
        };
        
        await _consolidationCore.ConsolidateAsync(digestible);
    }
    
    public async Task SendDigestAsync(ConsolidatedDigest digest)
    {
        var items = JsonConvert.DeserializeObject<List<EmailPreview>>(
            digest.DigestBodyJson);
            
        var emailBody = BuildDigestEmail(items, digest);
        
        await _emailService.SendAsync(new Email
        {
            To = digest.TargetId,
            Subject = $"Daily Digest - {items.Count} notifications",
            Body = emailBody
        });
    }
}
```

#### Activity Feed Deduplication

```csharp
public class ActivityFeedService
{
    private readonly DuplicateSuppressionCore _suppressionCore;
    private readonly IRepository<ActivityFeedItem> _feedRepo;
    
    public async Task AddActivityAsync(UserActivity activity)
    {
        // Create suppressible activity
        var suppressible = new SuppressibleActivity
        {
            Id = activity.Id,
            UserId = activity.UserId,
            ActivityType = activity.Type,
            EntityId = activity.EntityId,
            EntityType = activity.EntityType,
            
            // Suppress identical activities for 5 minutes
            SuppressionTimeMinutes = 5
        };
        
        // Check suppression
        if (await _suppressionCore.ShouldSuppress(suppressible))
        {
            // Update existing activity instead of creating new
            var existing = await _feedRepo.GetOneAsync(
                f => f.UserId == activity.UserId &&
                     f.ActivityType == activity.Type &&
                     f.EntityId == activity.EntityId);
                     
            if (existing != null)
            {
                existing.UpdateCount++;
                existing.LastOccurred = DateTime.UtcNow;
                await _feedRepo.UpdateAsync(existing);
            }
            
            return;
        }
        
        // Add new activity
        await _feedRepo.CreateAsync(new ActivityFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = activity.UserId,
            ActivityType = activity.Type,
            EntityId = activity.EntityId,
            EntityType = activity.EntityType,
            FirstOccurred = DateTime.UtcNow,
            LastOccurred = DateTime.UtcNow,
            UpdateCount = 1
        });
    }
}
```

#### Error Report Consolidation

```csharp
public class ErrorReportingService
{
    private readonly ConsolidationCore _consolidationCore;
    private readonly DuplicateSuppressionCore _suppressionCore;
    
    public async Task ReportErrorAsync(ApplicationError error)
    {
        // First check if we should suppress this exact error
        var suppressible = new SuppressibleError(error)
        {
            SuppressionTimeMinutes = 15 // Suppress identical errors for 15 min
        };
        
        if (await _suppressionCore.ShouldSuppress(suppressible))
        {
            // Increment counter for existing error
            await IncrementErrorCountAsync(error);
            return;
        }
        
        // Then add to hourly digest
        var digestible = new DigestibleError
        {
            Id = error.Id,
            ErrorType = error.Type,
            Source = error.Source,
            
            DigestUntil = DateTime.UtcNow.AddHours(1),
            DigestBodyJson = JsonConvert.SerializeObject(new
            {
                error.Type,
                error.Message,
                error.StackTrace,
                error.Timestamp
            }),
            
            HeadLimit = 10,
            TailLimit = 5,
            
            ForwardToExchange = "error-reports",
            ForwardToRoute = "hourly-summary"
        };
        
        await _consolidationCore.ConsolidateAsync(digestible);
    }
}
```

### Advanced Patterns

#### Custom Suppression Rules

```csharp
public class AdvancedSuppressionService
{
    private readonly DuplicateSuppressionCore _core;
    
    public async Task<bool> ShouldSuppressWithRules(
        ICanShutUp item,
        Func<SuppressedItem, bool> customRule)
    {
        var suppressions = await _core.GetActiveSuppressions(
            item.ComparisonType);
            
        foreach (var suppression in suppressions)
        {
            // Apply custom rule
            if (customRule(suppression))
            {
                return true;
            }
        }
        
        return false;
    }
}

// Usage: Suppress if similar error from same user
var shouldSuppress = await service.ShouldSuppressWithRules(
    errorItem,
    suppression => 
    {
        var data = JsonConvert.DeserializeObject<ErrorData>(
            suppression.ComparisonString);
        return data.UserId == currentUserId && 
               data.ErrorCode == errorItem.ErrorCode;
    });
```

#### Digest with Priority

```csharp
public class PriorityDigestService
{
    public async Task AddToDigestWithPriorityAsync(
        IDigestible item,
        DigestPriority priority)
    {
        var digest = await _consolidationCore.FindDigestAsync(
            item.ComparisonType,
            item.DigestUntil);
            
        // Add with priority
        digest.Items.Add(new PriorityDigestItem
        {
            Item = new DigestItem
            {
                ItemId = item.TargetId,
                ComparisonHash = item.ComparisonHash,
                Details = item.DigestBodyJson,
                AddedAt = DateTime.UtcNow
            },
            Priority = priority
        });
        
        // Check if high priority items should trigger immediate send
        var highPriorityCount = digest.Items
            .Count(i => i.Priority == DigestPriority.High);
            
        if (highPriorityCount >= 3)
        {
            await _consolidationCore.SendDigestImmediatelyAsync(digest);
        }
    }
}
```

### Best Practices

1. **Choose appropriate suppression windows** - Balance between deduplication and responsiveness
2. **Design comparison properties carefully** - Include only relevant fields
3. **Use efficient hash algorithms** - Consider performance for high-volume scenarios
4. **Implement digest cleanup** - Remove old digests to prevent storage growth
5. **Monitor suppression effectiveness** - Track suppression rates and adjust
6. **Handle time zones properly** - Consider user preferences for digest timing
7. **Provide override mechanisms** - Allow forcing through suppression when needed
8. **Test edge cases** - Handle clock skew, system restarts
9. **Document suppression behavior** - Make it clear to users why items are grouped
10. **Consider scalability** - Design for distributed suppression tracking

---

## Diagnostics System

The Diagnostics System provides comprehensive application monitoring, performance tracking, and alerting capabilities. It enables developers to measure performance, track metrics, and receive alerts about application health and issues.

### Core Components

#### IApplicationAlert Interface
Interface for raising application alerts:

```csharp
public interface IApplicationAlert
{
    void RaiseAlert(
        ApplicationAlertKind kind, 
        LogLevel severity,
        string details,
        int limitIn15 = 0,
        string limitGroup = "",  
        [CallerFilePath] string file = "unknown",
        [CallerLineNumber] int line = -1, 
        [CallerMemberName] string member = "unknown");
        
    void RaiseAlert(ApplicationAlertKind general, LogLevel information, object p);
}
```

#### ApplicationAlertKind Enum
Categories of application alerts:

```csharp
public enum ApplicationAlertKind
{
    General,
    Security,
    Performance,
    DataIntegrity,
    Services,
    ThirdParty,
    Configuration,
    UserAction
}
```

#### PerfTrack
Performance measurement utility:

```csharp
public class PerfTrack
{
    private readonly Stopwatch _sw = new();
    private string _nm = "ERROR";
    private string _file = "unknown";
    private int _ln;
    
    public void Begin(string nm, 
        [CallerFilePath] string file = "unknown", 
        [CallerLineNumber] int ln = 0)
    {
        _nm = nm;
        _file = file;
        _ln = ln;
        _sw.Reset();
        _sw.Start();
    }
    
    public void End()
    {
        _sw.Stop();
        PerformanceMetric.IncRate(_nm, _file, _ln, _sw.ElapsedMilliseconds);
    }
    
    public static IDisposable Stopwatch(string nm, 
        [CallerFilePath] string file = "unknown", 
        [CallerLineNumber] int ln = 0)
    {
        var pt = new PerfTrack();
        pt.Begin(nm, file, ln);
        return Disposable.Create(() => pt.End());
    }
}
```

#### PerformanceMetric
Static class for tracking performance metrics:

```csharp
public static class PerformanceMetric
{
    private static readonly ConcurrentDictionary<string, PerfRateTrack> _rates = new();
    
    public static void IncRate(string name, string file, int ln, double ms)
    {
        if (!_rates.ContainsKey(name))
            _rates[name] = new PerfRateTrack 
            {
                Name = name, 
                Count = 0, 
                Starting = DateTime.UtcNow, 
                MaxMs = 0.0, 
                File = file, 
                Line = ln
            };
        
        var current = _rates[name];
        current.MachineName = Environment.MachineName;
        current.Count += 1;
        
        if (ms > current.MaxMs)
            current.MaxMs = ms;
        if (current.MinMs == 0 || current.MinMs > ms)
            current.MinMs = ms;
            
        current.RecordReading(ms);
    }
    
    public static PerfRateTrack[] Clear()
    {
        var retval = _rates.Values.ToArray();
        _rates.Clear();
        return retval;
    }
}
```

#### PerfRateTrack
Performance statistics tracking:

```csharp
public class PerfRateTrack
{
    public string Name { get; set; }
    public string MachineName { get; set; }
    public string File { get; set; }
    public int Line { get; set; }
    public DateTime Starting { get; set; }
    public long Count { get; set; }
    public double MaxMs { get; set; }
    public double MinMs { get; set; }
    public double Sum { get; set; }
    public double Average => Count > 0 ? Sum / Count : 0;
    public double Median { get; private set; }
    
    private readonly List<double> _readings = new();
    private readonly object _lock = new();
    
    public void RecordReading(double ms)
    {
        lock (_lock)
        {
            _readings.Add(ms);
            Sum += ms;
            
            // Calculate median
            if (_readings.Count > 0)
            {
                var sorted = _readings.OrderBy(x => x).ToList();
                var mid = sorted.Count / 2;
                Median = sorted.Count % 2 == 0 
                    ? (sorted[mid - 1] + sorted[mid]) / 2 
                    : sorted[mid];
            }
        }
    }
}
```

### Alert Implementations

#### SimpleApplicationAlert
Basic console-based alert implementation:

```csharp
public class SimpleApplicationAlert : IApplicationAlert
{
    private readonly ConcurrentDictionary<string, DateTime> _limitGroups = new();
    
    public void RaiseAlert(
        ApplicationAlertKind kind, 
        LogLevel severity,
        string details,
        int limitIn15 = 0,
        string limitGroup = "",
        string file = "unknown",
        int line = -1,
        string member = "unknown")
    {
        // Check rate limiting
        if (limitIn15 > 0 && !string.IsNullOrEmpty(limitGroup))
        {
            var key = $"{limitGroup}:{kind}:{severity}";
            if (_limitGroups.TryGetValue(key, out var lastAlert))
            {
                if ((DateTime.UtcNow - lastAlert).TotalMinutes < 15)
                    return;
            }
            _limitGroups[key] = DateTime.UtcNow;
        }
        
        // Format and output alert
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var location = $"{file}:{line} in {member}";
        
        Console.WriteLine($"[{timestamp}] {kind} - {severity}: {details}");
        Console.WriteLine($"  Location: {location}");
    }
}
```

#### SwitchingApplicationAlert
Alert implementation that switches between multiple backends:

```csharp
public class SwitchingApplicationAlert : IApplicationAlert
{
    private readonly IApplicationAlert[] _alerts;
    private readonly SwitchingApplicationAlertOptions _options;
    
    public SwitchingApplicationAlert(
        IOptions<SwitchingApplicationAlertOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _alerts = _options.AlertTypes
            .Select(type => (IApplicationAlert)serviceProvider.GetService(type))
            .Where(alert => alert != null)
            .ToArray();
    }
    
    public void RaiseAlert(
        ApplicationAlertKind kind, 
        LogLevel severity,
        string details,
        int limitIn15 = 0,
        string limitGroup = "",
        string file = "unknown",
        int line = -1,
        string member = "unknown")
    {
        // Send to all configured alert backends
        Parallel.ForEach(_alerts, alert =>
        {
            try
            {
                alert.RaiseAlert(kind, severity, details, 
                    limitIn15, limitGroup, file, line, member);
            }
            catch (Exception ex)
            {
                // Log but don't throw - alerting should not break the app
                Console.WriteLine($"Alert backend failed: {ex.Message}");
            }
        });
    }
}
```

### Performance Tracking Examples

#### Basic Performance Measurement

```csharp
public class DataService
{
    private readonly IRepository<DataModel> _repository;
    
    public async Task<List<DataModel>> GetDataAsync(string filter)
    {
        using (PerfTrack.Stopwatch("DataService.GetData"))
        {
            var results = await _repository.GetAllAsync(
                d => d.Name.Contains(filter));
            
            return results.Items;
        }
    }
    
    public async Task ProcessBatchAsync(List<DataModel> items)
    {
        var tracker = new PerfTrack();
        tracker.Begin("DataService.ProcessBatch");
        
        try
        {
            foreach (var item in items)
            {
                using (PerfTrack.Stopwatch($"ProcessItem:{item.Type}"))
                {
                    await ProcessItemAsync(item);
                }
            }
        }
        finally
        {
            tracker.End();
        }
    }
}
```

#### Performance Monitoring Service

```csharp
public class PerformanceMonitoringService : BackgroundService
{
    private readonly IPerformanceMetricPersistence _persistence;
    private readonly IApplicationAlert _alerts;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            
            // Collect and analyze metrics
            var metrics = PerformanceMetric.Clear();
            
            foreach (var metric in metrics)
            {
                // Check for performance degradation
                if (metric.Average > 1000) // 1 second
                {
                    _alerts.RaiseAlert(
                        ApplicationAlertKind.Performance,
                        LogLevel.Warning,
                        $"Slow operation detected: {metric.Name} " +
                        $"avg: {metric.Average}ms, max: {metric.MaxMs}ms",
                        limitIn15: 3,
                        limitGroup: metric.Name);
                }
                
                // Persist metrics
                await _persistence.SaveMetricAsync(metric);
            }
        }
    }
}
```

#### Request Performance Middleware

```csharp
public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApplicationAlert _alerts;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var method = context.Request.Method;
        
        using (PerfTrack.Stopwatch($"HTTP:{method}:{path}"))
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                
                // Alert on slow requests
                if (sw.ElapsedMilliseconds > 5000)
                {
                    _alerts.RaiseAlert(
                        ApplicationAlertKind.Performance,
                        LogLevel.Warning,
                        $"Slow request: {method} {path} took {sw.ElapsedMilliseconds}ms",
                        limitIn15: 5,
                        limitGroup: $"{method}:{path}");
                }
            }
        }
    }
}
```

### Alert Usage Examples

#### Security Alerts

```csharp
public class SecurityService
{
    private readonly IApplicationAlert _alerts;
    
    public async Task<bool> ValidateAccessAsync(User user, Resource resource)
    {
        if (user.FailedLoginAttempts > 5)
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Security,
                LogLevel.Warning,
                $"Multiple failed login attempts for user {user.Email}",
                limitIn15: 1,
                limitGroup: $"failed-login:{user.Id}");
                
            return false;
        }
        
        if (IsUnusualAccessPattern(user, resource))
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.Security,
                LogLevel.Information,
                $"Unusual access pattern detected: User {user.Email} " +
                $"accessing {resource.Name} from {user.LastIpAddress}");
        }
        
        return true;
    }
}
```

#### Data Integrity Alerts

```csharp
public class DataIntegrityService
{
    private readonly IApplicationAlert _alerts;
    private readonly IRepository<DataRecord> _repository;
    
    public async Task ValidateDataIntegrityAsync()
    {
        var records = await _repository.GetAllAsync();
        var issues = new List<string>();
        
        foreach (var record in records.Items)
        {
            if (string.IsNullOrEmpty(record.RequiredField))
            {
                issues.Add($"Record {record.Id} missing required field");
            }
            
            if (record.CreatedDate > record.UpdatedDate)
            {
                issues.Add($"Record {record.Id} has invalid timestamps");
            }
        }
        
        if (issues.Any())
        {
            _alerts.RaiseAlert(
                ApplicationAlertKind.DataIntegrity,
                LogLevel.Error,
                $"Data integrity issues found: {string.Join(", ", issues.Take(5))}... " +
                $"({issues.Count} total issues)",
                limitIn15: 1,
                limitGroup: "data-integrity-check");
        }
    }
}
```

#### Service Health Monitoring

```csharp
public class HealthMonitoringService
{
    private readonly IApplicationAlert _alerts;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task CheckExternalServicesAsync()
    {
        var services = new[]
        {
            ("Payment API", "https://payment-api.example.com/health"),
            ("Email Service", "https://email.example.com/health"),
            ("Storage Service", "https://storage.example.com/health")
        };
        
        foreach (var (name, url) in services)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _alerts.RaiseAlert(
                        ApplicationAlertKind.ThirdParty,
                        LogLevel.Error,
                        $"{name} health check failed: {response.StatusCode}",
                        limitIn15: 3,
                        limitGroup: $"health:{name}");
                }
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(
                    ApplicationAlertKind.ThirdParty,
                    LogLevel.Critical,
                    $"{name} is unreachable: {ex.Message}",
                    limitIn15: 1,
                    limitGroup: $"health:{name}");
            }
        }
    }
}
```

### Performance Reporting

```csharp
public class PerformanceReportGenerator
{
    private readonly IPerformanceMetricPersistence _persistence;
    
    public async Task<PerformanceReport> GenerateReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var metrics = await _persistence.GetMetricsAsync(startDate, endDate);
        
        return new PerformanceReport
        {
            Period = new DateRange(startDate, endDate),
            
            TopSlowestOperations = metrics
                .OrderByDescending(m => m.MaxMs)
                .Take(10)
                .Select(m => new OperationSummary
                {
                    Name = m.Name,
                    MaxDuration = m.MaxMs,
                    AvgDuration = m.Average,
                    CallCount = m.Count,
                    Location = $"{m.File}:{m.Line}"
                })
                .ToList(),
                
            MostFrequentOperations = metrics
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList(),
                
            PerformanceTrends = CalculateTrends(metrics),
            
            Recommendations = GenerateRecommendations(metrics)
        };
    }
}
```

### Best Practices

1. **Use appropriate alert levels** - Critical for immediate action, Warning for investigation
2. **Implement rate limiting** - Prevent alert fatigue with limitIn15 parameter
3. **Include context in alerts** - File, line, and member information for debugging
4. **Measure critical paths** - Focus on user-facing operations
5. **Set performance budgets** - Alert when operations exceed thresholds
6. **Aggregate metrics** - Collect and analyze patterns over time
7. **Use structured logging** - Make alerts searchable and filterable
8. **Monitor external dependencies** - Track third-party service health
9. **Implement alert routing** - Send different alerts to appropriate teams
10. **Regular metric cleanup** - Prevent unbounded growth of metric storage

---