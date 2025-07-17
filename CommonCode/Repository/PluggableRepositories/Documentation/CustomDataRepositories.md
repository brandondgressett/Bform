# Custom Data Repositories Tutorial

This tutorial demonstrates how to create custom data repositories using the BFormDomain pluggable repository framework. The framework provides base classes and infrastructure to easily create repositories that automatically generate events for all CRUD operations.

## Table of Contents

1. [Overview](#overview)
2. [Basic Concepts](#basic-concepts)
3. [Creating a Custom MongoDB Repository](#creating-a-custom-mongodb-repository)
4. [Creating an External Data Source Repository (RSS Feed)](#creating-an-external-data-source-repository-rss-feed)
5. [Event Configuration](#event-configuration)
6. [Dependency Injection Setup](#dependency-injection-setup)
7. [Using Repository Interceptors](#using-repository-interceptors)
8. [Best Practices](#best-practices)
9. [Complete Examples](#complete-examples)

## Overview

The BFormDomain pluggable repository framework provides:

- **EventAwareRepository<T>**: Base class for MongoDB repositories with automatic event generation
- **ExternalDataSourceRepository<T>**: Base class for external data sources (APIs, RSS feeds, files)
- **RepositoryEventConfiguration**: Fine-grained control over event generation
- **IEventAwareEntity**: Interface for entities to provide custom event data
- **RepositoryEventInterceptor<T>**: Decorator for adding cross-cutting concerns
- **CustomRepositoryRegistration**: Extension methods for easy DI registration

## Basic Concepts

### Event-Aware Entities

Entities can implement `IEventAwareEntity` to control how events are generated:

```csharp
public class Product : EventAwareEntityBase
{
    public override string EntityType { get; set; } = "Product";
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockLevel { get; set; }

    // Custom event data for specific operations
    public override object? GetEventData(string operation)
    {
        return operation switch
        {
            "create" => new { ProductCreated = true, InitialStock = StockLevel },
            "update" => new { PriceChanged = Price, StockLevel },
            "delete" => new { ProductRemoved = true, FinalStock = StockLevel },
            _ => null
        };
    }

    // Control event generation based on business rules
    public override bool ShouldGenerateEvent(string operation)
    {
        // Don't generate events for products with zero stock being deleted
        if (operation == "delete" && StockLevel == 0)
            return false;
            
        return base.ShouldGenerateEvent(operation);
    }

    // Custom event topics
    public override string? GetCustomEventTopic(string operation)
    {
        return operation switch
        {
            "create" => "Inventory.Product.Created",
            "update" => "Inventory.Product.Updated", 
            "delete" => "Inventory.Product.Deleted",
            _ => null
        };
    }

    // Event priority (0-100, where 100 is highest)
    public override int GetEventPriority(string operation)
    {
        // High priority for low stock updates
        if (operation == "update" && StockLevel < 10)
            return 90;
            
        return base.GetEventPriority(operation);
    }
}
```

### Event Configuration

Configure event generation behavior:

```csharp
var eventConfig = new RepositoryEventConfiguration
{
    GenerateCreateEvents = true,
    GenerateUpdateEvents = true,
    GenerateDeleteEvents = true,
    CreateEventTopic = "Product.Created",
    UpdateEventTopic = "Product.Updated", 
    DeleteEventTopic = "Product.Deleted",
    IncludeOldStateInEvents = true,
    TrackPropertyChanges = true,
    EventFilter = (entity, operation) => 
    {
        // Custom logic to determine if event should be generated
        var product = entity as Product;
        return product?.StockLevel > 0 || operation != "delete";
    }
};
```

## Creating a Custom MongoDB Repository

### Step 1: Define the Repository Interface

```csharp
public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> GetLowStockProductsAsync(int threshold = 10);
    Task<List<Product>> GetProductsByCategoryAsync(string category);
    Task<decimal> GetAveragePriceAsync();
    Task UpdateStockLevelAsync(Guid productId, int newStockLevel);
}
```

### Step 2: Implement the Repository

```csharp
public class ProductRepository : EventAwareRepository<Product>, IProductRepository
{
    public ProductRepository(
        ITenantContext tenantContext,
        ITenantConnectionProvider connectionProvider,
        IOptions<MultiTenancyOptions> multiTenancyOptions,
        IRepository<AppEvent> eventRepository,
        TenantAwareEventFactory eventFactory,
        SimpleApplicationAlert alerts,
        ILogger<ProductRepository> logger)
        : base(tenantContext, connectionProvider, multiTenancyOptions, 
               eventRepository, eventFactory, alerts, logger)
    {
    }

    // Custom event configuration for products
    protected override RepositoryEventConfiguration CreateEventConfiguration()
    {
        return new RepositoryEventConfiguration
        {
            GenerateCreateEvents = true,
            GenerateUpdateEvents = true,
            GenerateDeleteEvents = true,
            CreateEventTopic = "Inventory.Product.Created",
            UpdateEventTopic = "Inventory.Product.Updated",
            DeleteEventTopic = "Inventory.Product.Deleted",
            IncludeOldStateInEvents = true,
            TrackPropertyChanges = true,
            EventFilter = (entity, operation) =>
            {
                var product = entity as Product;
                // Don't generate delete events for zero-stock products
                return !(operation == "delete" && product?.StockLevel == 0);
            }
        };
    }

    // Business logic methods
    public async Task<List<Product>> GetLowStockProductsAsync(int threshold = 10)
    {
        var (products, _) = await GetAllAsync(p => p.StockLevel <= threshold);
        return products;
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        var (products, _) = await GetAllAsync(p => p.Category == category);
        return products;
    }

    public async Task<decimal> GetAveragePriceAsync()
    {
        var (products, _) = await GetAllAsync(p => true);
        return products.Any() ? products.Average(p => p.Price) : 0;
    }

    public async Task UpdateStockLevelAsync(Guid productId, int newStockLevel)
    {
        var (product, _) = await LoadAsync(productId);
        var oldStock = product.StockLevel;
        product.StockLevel = newStockLevel;
        
        await UpdateAsync(product);
        
        // Custom event for stock level changes
        if (oldStock != newStockLevel)
        {
            await GenerateStockLevelChangeEventAsync(product, oldStock, newStockLevel);
        }
    }

    // Custom event generation
    private async Task GenerateStockLevelChangeEventAsync(Product product, int oldStock, int newStock)
    {
        var appEvent = new AppEvent
        {
            Topic = "Inventory.Stock.Changed",
            ActionId = "StockUpdate",
            OriginEntityType = product.EntityType,
            OriginEntityId = product.Id,
            EntityPayload = MongoDB.Bson.BsonDocument.Parse(
                Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    OldStock = oldStock,
                    NewStock = newStock,
                    StockChange = newStock - oldStock,
                    IsLowStock = newStock < 10,
                    IsOutOfStock = newStock == 0
                }))
        };

        await EventRepository.CreateAsync(appEvent);
    }

    // Override to customize create event data
    protected override Task<object?> GetCreateEventDataAsync(Product entity)
    {
        return Task.FromResult<object?>(new
        {
            ProductName = entity.Name,
            Category = entity.Category,
            InitialPrice = entity.Price,
            InitialStock = entity.StockLevel,
            CreatedAt = DateTime.UtcNow
        });
    }

    // Override to customize update event data
    protected override Task<object?> GetUpdateEventDataAsync(Product entity, Product? oldState)
    {
        var eventData = new Dictionary<string, object?>
        {
            ["ProductName"] = entity.Name,
            ["CurrentPrice"] = entity.Price,
            ["CurrentStock"] = entity.StockLevel,
            ["UpdatedAt"] = DateTime.UtcNow
        };

        if (oldState != null)
        {
            eventData["Changes"] = new
            {
                PriceChanged = oldState.Price != entity.Price,
                StockChanged = oldState.StockLevel != entity.StockLevel,
                NameChanged = oldState.Name != entity.Name,
                CategoryChanged = oldState.Category != entity.Category
            };
        }

        return Task.FromResult<object?>(eventData);
    }
}
```

## Creating an External Data Source Repository (RSS Feed)

### Step 1: Define the RSS Feed Entity

```csharp
public class RssFeedItem : EventAwareEntityBase
{
    public override string EntityType { get; set; } = "RssFeedItem";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string Author { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public string FeedSource { get; set; } = string.Empty;

    // Custom event data
    public override object? GetEventData(string operation)
    {
        return new
        {
            Title,
            FeedSource,
            PublishedDate,
            Operation = operation,
            Categories
        };
    }

    // Custom event topics
    public override string? GetCustomEventTopic(string operation)
    {
        return operation switch
        {
            "create" => "RSS.NewItem",
            "update" => "RSS.ItemUpdated",
            "delete" => "RSS.ItemRemoved",
            _ => null
        };
    }
}
```

### Step 2: Implement the RSS Feed Repository

```csharp
public interface IRssFeedRepository : IRepository<RssFeedItem>
{
    Task<List<RssFeedItem>> GetItemsBySourceAsync(string feedSource);
    Task<List<RssFeedItem>> GetRecentItemsAsync(TimeSpan timeSpan);
    Task ForceSyncAsync();
}

public class RssFeedRepository : ExternalDataSourceRepository<RssFeedItem>, IRssFeedRepository
{
    private readonly string _feedUrl;
    private readonly HttpClient _httpClient;

    public RssFeedRepository(
        IRepository<AppEvent> eventRepository,
        TenantAwareEventFactory eventFactory,
        ITenantContext tenantContext,
        ILogger<RssFeedRepository> logger,
        HttpClient httpClient,
        string feedUrl,
        TimeSpan? pollingInterval = null)
        : base(eventRepository, eventFactory, tenantContext, logger, 
               pollingInterval ?? TimeSpan.FromMinutes(15))
    {
        _feedUrl = feedUrl;
        _httpClient = httpClient;
    }

    protected override bool SupportsWriteOperations => false;

    protected override string GetExternalSourceType() => "RSS";

    // Fetch data from RSS feed
    protected override async Task<IEnumerable<RssFeedItem>> FetchDataFromSourceAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_feedUrl, cancellationToken);
            return ParseRssFeed(response);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to fetch RSS feed from {FeedUrl}", _feedUrl);
            return Enumerable.Empty<RssFeedItem>();
        }
    }

    private List<RssFeedItem> ParseRssFeed(string xmlContent)
    {
        var items = new List<RssFeedItem>();
        
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
            var feedItems = doc.Descendants("item");

            foreach (var item in feedItems)
            {
                var rssItem = new RssFeedItem
                {
                    Id = Guid.NewGuid(),
                    Title = item.Element("title")?.Value ?? string.Empty,
                    Description = item.Element("description")?.Value ?? string.Empty,
                    Link = item.Element("link")?.Value ?? string.Empty,
                    Author = item.Element("author")?.Value ?? string.Empty,
                    FeedSource = _feedUrl,
                    PublishedDate = DateTime.TryParse(item.Element("pubDate")?.Value, out var pubDate) 
                        ? pubDate 
                        : DateTime.UtcNow,
                    Categories = item.Elements("category")
                        .Select(c => c.Value)
                        .ToList(),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                // Generate consistent ID based on link
                rssItem.Id = GenerateConsistentId(rssItem.Link);
                items.Add(rssItem);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to parse RSS feed content");
        }

        return items;
    }

    private Guid GenerateConsistentId(string link)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(link));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }

    // Custom change detection
    protected override bool HasChanged(RssFeedItem oldItem, RssFeedItem newItem)
    {
        return oldItem.Title != newItem.Title ||
               oldItem.Description != newItem.Description ||
               oldItem.PublishedDate != newItem.PublishedDate;
    }

    // Business logic methods
    public async Task<List<RssFeedItem>> GetItemsBySourceAsync(string feedSource)
    {
        var (items, _) = await GetAllAsync(item => item.FeedSource == feedSource);
        return items;
    }

    public async Task<List<RssFeedItem>> GetRecentItemsAsync(TimeSpan timeSpan)
    {
        var cutoffDate = DateTime.UtcNow - timeSpan;
        var (items, _) = await GetAllAsync(item => item.PublishedDate >= cutoffDate);
        return items.OrderByDescending(item => item.PublishedDate).ToList();
    }

    public async Task ForceSyncAsync()
    {
        await ForceSyncAsync(CancellationToken.None);
    }

    // Not implemented for RSS (read-only)
    protected override Task<RssFeedItem> CreateInExternalSourceAsync(
        RssFeedItem entity, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("RSS feeds are read-only");
    }

    protected override Task<RssFeedItem> UpdateInExternalSourceAsync(
        RssFeedItem entity, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("RSS feeds are read-only");
    }

    protected override Task DeleteFromExternalSourceAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("RSS feeds are read-only");
    }

    protected override RssFeedItem MapExternalDataToEntity(object externalData)
    {
        return (RssFeedItem)externalData;
    }
}
```

## Event Configuration

### Global Event Configuration

```csharp
services.ConfigureRepositoryEvents(config =>
{
    config.GenerateCreateEvents = true;
    config.GenerateUpdateEvents = true;
    config.GenerateDeleteEvents = true;
    config.GenerateBatchEvents = true;
    config.GenerateSystemOperationEvents = false; // Don't generate events for system operations
});
```

### Repository-Specific Configuration

```csharp
protected override RepositoryEventConfiguration CreateEventConfiguration()
{
    return new RepositoryEventConfiguration
    {
        GenerateCreateEvents = true,
        GenerateUpdateEvents = true,
        GenerateDeleteEvents = false, // Don't track deletions for this entity
        CreateEventTopic = "CustomEntity.Created",
        UpdateEventTopic = "CustomEntity.Updated",
        IncludeOldStateInEvents = true,
        TrackPropertyChanges = true,
        MaxBatchEventCount = 50,
        EventFilter = (entity, operation) =>
        {
            // Custom logic for when to generate events
            return true;
        },
        CustomEventDataProvider = (entity, operation) =>
        {
            // Custom data to include in all events
            return new { Timestamp = DateTime.UtcNow, Source = "CustomRepository" };
        }
    };
}
```

## Dependency Injection Setup

### Registering Event-Aware Repositories

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register event-aware MongoDB repository
    services.AddEventAwareRepository<IProductRepository, ProductRepository, Product>();

    // Register external data source repository with custom polling interval
    services.AddExternalDataSourceRepository<IRssFeedRepository, RssFeedRepository, RssFeedItem>(
        pollingInterval: TimeSpan.FromMinutes(30));

    // Register custom repository with full configuration
    services.AddCustomRepository<IProductRepository, ProductRepository, Product>(options =>
    {
        options.RegisterAsGenericRepository = true;
        options.AddHealthCheck = true;
        options.HealthCheckTags = new List<string> { "products", "inventory" };
        options.EventConsumerType = typeof(ProductEventConsumer);
    });

    // Register repositories from assembly
    services.AddRepositoriesFromAssembly(typeof(ProductRepository));

    // Add event consumers
    services.AddRepositoryEventConsumer<ProductEventConsumer, Product>();
    services.AddRepositoryEventConsumer<RssFeedEventConsumer, RssFeedItem>();

    // Add change detection
    services.AddChangeDetection<Product>(ChangeDetectionStrategy.Hash);
    services.AddChangeDetection<RssFeedItem>(ChangeDetectionStrategy.Timestamp);
}
```

### Manual Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Basic repository registration
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddSingleton<IRssFeedRepository, RssFeedRepository>();

    // Register RSS repository as hosted service for polling
    services.AddSingleton<IHostedService>(provider =>
        provider.GetRequiredService<IRssFeedRepository>());

    // Configure HTTP client for RSS repository
    services.AddHttpClient<RssFeedRepository>();

    // Configure options
    services.Configure<ExternalDataSourceOptions>(options =>
    {
        options.DefaultPollingInterval = TimeSpan.FromMinutes(15);
        options.MaxRetryAttempts = 3;
        options.RetryDelay = TimeSpan.FromSeconds(30);
    });
}
```

## Using Repository Interceptors

### Adding Cross-Cutting Concerns

```csharp
// Create an intercepted repository
var interceptedRepository = RepositoryEventInterceptor<Product>
    .Create(originalRepository)
    .WithLogging(logger)
    .WithValidation(productValidator)
    .WithEventGeneration(eventRepository, eventConfig);

// Use fluent configuration
var repository = new RepositoryEventInterceptor<Product>(originalRepository)
    .WithLogging(loggerFactory.CreateLogger<LoggingInterceptor<Product>>())
    .WithValidation(new ProductValidator())
    .WithEventGeneration(eventRepository);

// Access the original repository if needed
var originalRepo = repository.InnerRepository;
```

### Custom Interceptors

```csharp
public class CachingInterceptor<T> : RepositoryInterceptorBase<T> 
    where T : class, IDataModel, IAppEntity
{
    private readonly IMemoryCache _cache;

    public CachingInterceptor(IMemoryCache cache)
    {
        _cache = cache;
    }

    public override async Task OnAfterCreateAsync(T entity, ITransactionContext? tc = null)
    {
        // Invalidate cache after create
        _cache.Remove($"{typeof(T).Name}_all");
        _cache.Set($"{typeof(T).Name}_{entity.Id}", entity, TimeSpan.FromMinutes(30));
    }

    public override async Task OnAfterUpdateAsync(T entity, ITransactionContext? tc = null)
    {
        // Update cache after update
        _cache.Remove($"{typeof(T).Name}_all");
        _cache.Set($"{typeof(T).Name}_{entity.Id}", entity, TimeSpan.FromMinutes(30));
    }

    public override async Task OnAfterDeleteAsync(T entity, ITransactionContext? tc = null)
    {
        // Remove from cache after delete
        _cache.Remove($"{typeof(T).Name}_all");
        _cache.Remove($"{typeof(T).Name}_{entity.Id}");
    }
}

// Usage
var cachedRepository = new RepositoryEventInterceptor<Product>(originalRepository)
    .AddInterceptor(new CachingInterceptor<Product>(memoryCache));
```

## Best Practices

### 1. Entity Design

```csharp
// Good: Inherit from EventAwareEntityBase for convenience
public class Product : EventAwareEntityBase
{
    public override string EntityType { get; set; } = "Product";
    // ... properties
    
    // Override only the methods you need
    public override bool ShouldGenerateEvent(string operation)
    {
        // Business logic for event generation
        return base.ShouldGenerateEvent(operation);
    }
}

// Good: Implement IEventAwareEntity directly for full control
public class CustomEntity : IEventAwareEntity
{
    // Implement all required properties and methods
}
```

### 2. Event Configuration

```csharp
// Good: Use meaningful event topics
CreateEventTopic = "Inventory.Product.Created",
UpdateEventTopic = "Inventory.Product.Updated",
DeleteEventTopic = "Inventory.Product.Deleted",

// Good: Configure appropriate event data
EventFilter = (entity, operation) =>
{
    // Only generate events for significant changes
    return entity is Product p && p.StockLevel > 0;
},

// Good: Include relevant context in events
CustomEventDataProvider = (entity, operation) =>
{
    return new 
    {
        Timestamp = DateTime.UtcNow,
        OperationType = operation,
        UserContext = _tenantContext.CurrentUser?.Id
    };
}
```

### 3. Error Handling

```csharp
// Good: Handle external data source errors gracefully
protected override async Task<IEnumerable<T>> FetchDataFromSourceAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        return await DoFetchAsync(cancellationToken);
    }
    catch (HttpRequestException ex)
    {
        Logger?.LogWarning(ex, "Network error fetching external data, returning cached data");
        return GetCachedData();
    }
    catch (Exception ex)
    {
        Logger?.LogError(ex, "Unexpected error fetching external data");
        return Enumerable.Empty<T>();
    }
}
```

### 4. Performance Considerations

```csharp
// Good: Use appropriate polling intervals
services.AddExternalDataSourceRepository<IRssFeedRepository, RssFeedRepository, RssFeedItem>(
    pollingInterval: TimeSpan.FromMinutes(30)); // Don't poll too frequently

// Good: Batch operations when possible
public async Task CreateMultipleProductsAsync(IEnumerable<Product> products)
{
    await CreateBatchAsync(products); // Generates batch events efficiently
}

// Good: Use change detection to avoid unnecessary processing
protected override bool HasChanged(Product oldItem, Product newItem)
{
    // Only check relevant properties
    return oldItem.Price != newItem.Price || 
           oldItem.StockLevel != newItem.StockLevel;
}
```

### 5. Testing

```csharp
// Good: Test with interceptors
[Test]
public async Task Should_Generate_Event_On_Product_Creation()
{
    var eventRepository = new Mock<IRepository<AppEvent>>();
    var interceptor = new EventGenerationInterceptor<Product>(
        eventRepository.Object, eventConfig);
    
    var repository = new RepositoryEventInterceptor<Product>(mockRepository.Object)
        .AddInterceptor(interceptor);
    
    await repository.CreateAsync(testProduct);
    
    eventRepository.Verify(r => r.CreateAsync(It.IsAny<AppEvent>()), Times.Once);
}

// Good: Test external data sources with mocked HTTP client
[Test]
public async Task Should_Parse_RSS_Feed_Correctly()
{
    var httpClient = CreateMockHttpClient(sampleRssXml);
    var repository = new RssFeedRepository(/*...dependencies...*/, httpClient, feedUrl);
    
    await repository.ForceSyncAsync();
    
    var items = await repository.GetRecentItemsAsync(TimeSpan.FromDays(1));
    Assert.That(items.Count, Is.EqualTo(5));
}
```

## Complete Examples

### Event Consumer

```csharp
public class ProductEventConsumer : IAppEventConsumer
{
    private readonly ILogger<ProductEventConsumer> _logger;
    private readonly IEmailService _emailService;

    public ProductEventConsumer(ILogger<ProductEventConsumer> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<bool> ConsumeAsync(AppEvent appEvent, CancellationToken cancellationToken)
    {
        try
        {
            switch (appEvent.Topic)
            {
                case "Inventory.Product.Created":
                    await HandleProductCreated(appEvent);
                    break;
                    
                case "Inventory.Stock.Changed":
                    await HandleStockChanged(appEvent);
                    break;
                    
                default:
                    return false; // Event not handled
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product event {EventId}", appEvent.Id);
            return false;
        }
    }

    private async Task HandleProductCreated(AppEvent appEvent)
    {
        var eventData = JObject.Parse(appEvent.JsonPayload);
        var productName = eventData["ProductName"]?.ToString();
        
        _logger.LogInformation("New product created: {ProductName}", productName);
        
        // Send notification to inventory managers
        await _emailService.SendAsync("inventory@company.com", 
            "New Product Created", 
            $"Product '{productName}' has been added to inventory.");
    }

    private async Task HandleStockChanged(AppEvent appEvent)
    {
        var eventData = JObject.Parse(appEvent.JsonPayload);
        var productName = eventData["ProductName"]?.ToString();
        var newStock = eventData["NewStock"]?.Value<int>() ?? 0;
        var isLowStock = eventData["IsLowStock"]?.Value<bool>() ?? false;

        if (isLowStock)
        {
            _logger.LogWarning("Low stock alert for product: {ProductName}, Stock: {Stock}", 
                productName, newStock);
                
            await _emailService.SendAsync("purchasing@company.com",
                "Low Stock Alert",
                $"Product '{productName}' is low on stock ({newStock} remaining).");
        }
    }
}
```

### Startup Configuration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure BFormDomain core services
        services.AddBFormDomain(options =>
        {
            options.EnableMultiTenancy = true;
            options.DefaultConnectionString = connectionString;
        });

        // Register custom repositories
        services.AddEventAwareRepository<IProductRepository, ProductRepository, Product>();
        
        // Register external data sources
        services.AddHttpClient<RssFeedRepository>();
        services.AddExternalDataSourceRepository<IRssFeedRepository, RssFeedRepository, RssFeedItem>(
            pollingInterval: TimeSpan.FromMinutes(30));

        // Configure global event settings
        services.ConfigureRepositoryEvents(config =>
        {
            config.GenerateCreateEvents = true;
            config.GenerateUpdateEvents = true;
            config.GenerateDeleteEvents = true;
            config.GenerateSystemOperationEvents = false;
        });

        // Register event consumers
        services.AddRepositoryEventConsumer<ProductEventConsumer, Product>();
        services.AddRepositoryEventConsumer<RssFeedEventConsumer, RssFeedItem>();

        // Add change detection
        services.AddChangeDetection<Product>(ChangeDetectionStrategy.Hash);
        services.AddChangeDetection<RssFeedItem>(ChangeDetectionStrategy.Timestamp);

        // Register interceptors
        services.AddScoped<LoggingInterceptor<Product>>();
        services.AddScoped<ValidationInterceptor<Product>>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<RepositoryHealthCheck<Product>>("products")
            .AddCheck<RepositoryHealthCheck<RssFeedItem>>("rss-feeds");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // ... other middleware
        
        app.UseHealthChecks("/health");
    }
}
```

This tutorial provides a comprehensive guide to creating custom data repositories with the BFormDomain framework. The framework handles event generation, multi-tenancy, caching, and provides extensive customization options for different data sources and business requirements.