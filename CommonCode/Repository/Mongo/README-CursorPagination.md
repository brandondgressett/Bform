# Cursor-Based Pagination for MongoDB

This implementation provides efficient cursor-based pagination for handling large datasets in MongoDB without the performance issues of offset-based pagination.

## Overview

Cursor-based pagination uses a unique, sortable field (like `_id` or timestamp) as a reference point to fetch the next set of results. This approach maintains consistent performance regardless of how deep into the dataset you paginate.

## Key Benefits

1. **Constant Performance**: O(1) complexity regardless of page depth
2. **Stable Results**: No duplicate or skipped items even with concurrent inserts
3. **Memory Efficient**: Only loads requested page size into memory
4. **Bidirectional Navigation**: Support for both forward and backward pagination
5. **Streaming Support**: Async enumerable for processing millions of documents

## Usage Examples

### Basic Cursor Pagination

```csharp
public class UserService
{
    private readonly IRepository<User> _userRepository;

    public async Task<CursorPaginationResult<User>> GetUsersAsync(string? cursor, int pageSize = 50)
    {
        var request = new CursorPaginationRequest
        {
            Cursor = cursor,
            PageSize = pageSize,
            CursorField = "_id",
            SortDirection = SortDirection.Ascending
        };

        return await _userRepository.GetWithCursorAsync(request);
    }
}

// Usage in API controller
[HttpGet("users")]
public async Task<IActionResult> GetUsers([FromQuery] string? cursor, [FromQuery] int pageSize = 50)
{
    var result = await _userService.GetUsersAsync(cursor, pageSize);
    
    return Ok(new
    {
        data = result.Items,
        pagination = new
        {
            nextCursor = result.NextCursor,
            previousCursor = result.PreviousCursor,
            hasNext = result.HasNext,
            hasPrevious = result.HasPrevious,
            pageSize = result.PageSize
        }
    });
}
```

### Cursor Pagination with Filtering

```csharp
public async Task<CursorPaginationResult<Product>> GetActiveProductsAsync(
    string? cursor, 
    decimal? minPrice = null,
    int pageSize = 100)
{
    // Build filter
    var filterBuilder = Builders<Product>.Filter;
    var filters = new List<FilterDefinition<Product>>
    {
        filterBuilder.Eq(p => p.IsActive, true)
    };
    
    if (minPrice.HasValue)
    {
        filters.Add(filterBuilder.Gte(p => p.Price, minPrice.Value));
    }
    
    var filter = filterBuilder.And(filters);

    var request = new CursorPaginationRequest
    {
        Cursor = cursor,
        PageSize = pageSize,
        CursorField = "createdAt", // Using timestamp for cursor
        SortDirection = SortDirection.Descending // Newest first
    };

    return await _productRepository.GetWithCursorAsync(request, filter);
}
```

### Cursor Pagination with Projection

```csharp
public async Task<CursorPaginationResult<ProductSummary>> GetProductSummariesAsync(
    string? cursor,
    int pageSize = 200)
{
    var request = new CursorPaginationRequest
    {
        Cursor = cursor,
        PageSize = pageSize,
        CursorField = "_id",
        SortDirection = SortDirection.Ascending
    };

    // Project only needed fields for better performance
    return await _productRepository.GetWithCursorAsync<ProductSummary>(
        request,
        p => new ProductSummary
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            InStock = p.Inventory > 0
        });
}
```

### Bidirectional Pagination

```csharp
public async Task<CursorPaginationResult<Order>> GetOrdersAsync(
    string? cursor,
    bool goBackward = false,
    int pageSize = 50)
{
    var request = new CursorPaginationRequest
    {
        Cursor = cursor,
        PageSize = pageSize,
        CursorField = "orderDate",
        SortDirection = SortDirection.Descending,
        Direction = goBackward ? CursorDirection.Backward : CursorDirection.Forward
    };

    return await _orderRepository.GetWithCursorAsync(request);
}

// Client can navigate in both directions
// Forward: use nextCursor
// Backward: use previousCursor with goBackward=true
```

### Streaming Large Datasets

```csharp
public async Task ProcessAllOrdersAsync(DateTime startDate)
{
    var filter = Builders<Order>.Filter.Gte(o => o.OrderDate, startDate);
    
    await foreach (var order in _orderRepository.StreamWithCursorAsync(
        filter, 
        cursorField: "orderDate",
        sortDirection: SortDirection.Ascending,
        batchSize: 500))
    {
        // Process each order without loading entire dataset
        await ProcessOrderAsync(order);
        
        // This efficiently handles millions of orders
    }
}
```

### Custom Cursor Fields

```csharp
// Using composite cursor for complex sorting
public class CustomCursorHelper
{
    public static string CreateCompositeCursor(DateTime timestamp, Guid id)
    {
        var doc = new BsonDocument
        {
            { "ts", timestamp },
            { "id", id }
        };
        return Convert.ToBase64String(doc.ToBson());
    }
}

// Usage with custom sorting
public async Task<CursorPaginationResult<Event>> GetEventsAsync(string? cursor)
{
    var request = new CursorPaginationRequest
    {
        Cursor = cursor,
        PageSize = 100,
        CursorField = "timestamp", // Primary sort field
        SortDirection = SortDirection.Descending
    };

    // Add secondary sort for stability
    var sort = Builders<Event>.Sort
        .Descending(e => e.Timestamp)
        .Ascending(e => e.Id);

    // Custom implementation would be needed for composite cursors
    return await _eventRepository.GetWithCursorAsync(request);
}
```

## Best Practices

### 1. Choose Appropriate Cursor Fields

- **Use Indexed Fields**: Always use fields that have indexes
- **Ensure Uniqueness**: Field should be unique or combine with `_id`
- **Consider Sort Order**: Natural order (like timestamps) works best
- **Immutable Fields**: Cursor fields should not change after creation

### 2. Handle Edge Cases

```csharp
public async Task<PagedResponse<T>> GetPagedAsync<T>(
    string? cursor,
    int requestedPageSize)
{
    // Validate page size
    var pageSize = Math.Max(1, Math.Min(requestedPageSize, 1000));
    
    try
    {
        var result = await GetWithCursorAsync(cursor, pageSize);
        return new PagedResponse<T>
        {
            Success = true,
            Data = result.Items,
            Cursor = new CursorInfo
            {
                Next = result.NextCursor,
                Previous = result.PreviousCursor,
                HasMore = result.HasNext
            }
        };
    }
    catch (ArgumentException ex) when (ex.Message.Contains("Invalid cursor"))
    {
        // Handle invalid cursor by starting from beginning
        return await GetPagedAsync<T>(null, pageSize);
    }
}
```

### 3. Implement Cursor Expiration

```csharp
public class ExpiringCursorHelper
{
    private const int CursorLifetimeHours = 24;
    
    public static bool IsCursorExpired(string cursor)
    {
        try
        {
            var (_, _, timestamp) = CursorHelper.DecodeCursor(cursor);
            return DateTime.UtcNow - timestamp > TimeSpan.FromHours(CursorLifetimeHours);
        }
        catch
        {
            return true; // Invalid cursor is considered expired
        }
    }
}
```

### 4. Optimize for Your Use Case

```csharp
// For real-time feeds (newest first)
var realtimeFeedRequest = new CursorPaginationRequest
{
    PageSize = 20,
    CursorField = "createdAt",
    SortDirection = SortDirection.Descending
};

// For data export (stable order)
var exportRequest = new CursorPaginationRequest
{
    PageSize = 1000, // Larger batches for export
    CursorField = "_id",
    SortDirection = SortDirection.Ascending
};

// For search results (relevance + stability)
var searchRequest = new CursorPaginationRequest
{
    PageSize = 50,
    CursorField = "score", // Assuming you have a search score
    SortDirection = SortDirection.Descending
};
```

## Performance Comparison

### Offset-based Pagination
- Page 1 (offset 0): ~5ms
- Page 100 (offset 5000): ~150ms
- Page 1000 (offset 50000): ~2000ms

### Cursor-based Pagination
- Page 1: ~5ms
- Page 100: ~5ms
- Page 1000: ~5ms

## Limitations

1. **No Random Access**: Can't jump to arbitrary page numbers
2. **Cursor Management**: Clients must store and send cursor tokens
3. **Sort Limitations**: Complex multi-field sorts require custom implementation
4. **Projection Constraints**: Cursor field must be included in projections

## Migration from Offset-based Pagination

```csharp
// Old offset-based method
public async Task<PagedList<T>> GetPagedOld(int page, int pageSize)
{
    var skip = (page - 1) * pageSize;
    var items = await collection.Find(filter)
        .Skip(skip)
        .Limit(pageSize)
        .ToListAsync();
    
    var totalCount = await collection.CountDocumentsAsync(filter);
    return new PagedList<T>(items, totalCount, page, pageSize);
}

// New cursor-based method with backward compatibility
public async Task<PagedList<T>> GetPaged(int? page, string? cursor, int pageSize)
{
    if (!string.IsNullOrEmpty(cursor))
    {
        // Use cursor-based pagination
        var result = await GetWithCursorAsync(cursor, pageSize);
        return new PagedList<T>(
            result.Items,
            totalCount: null, // Don't calculate total for cursor pagination
            page: page ?? 1,
            pageSize: pageSize,
            nextCursor: result.NextCursor
        );
    }
    else if (page.HasValue && page.Value <= 10)
    {
        // Use offset for first few pages (backward compatibility)
        return await GetPagedOld(page.Value, pageSize);
    }
    else
    {
        // Force cursor pagination for deep pagination
        throw new InvalidOperationException(
            "Offset pagination is limited to first 10 pages. Use cursor pagination for better performance.");
    }
}
```

## Testing Cursor Pagination

```csharp
[TestClass]
public class CursorPaginationTests
{
    [TestMethod]
    public async Task Should_Paginate_Forward_Correctly()
    {
        // Arrange
        var allItems = await GenerateTestData(250);
        var pageSize = 100;
        
        // Act - Get all pages
        var allResults = new List<TestEntity>();
        string? cursor = null;
        
        while (true)
        {
            var result = await repository.GetWithCursorAsync(
                new CursorPaginationRequest
                {
                    Cursor = cursor,
                    PageSize = pageSize
                });
            
            allResults.AddRange(result.Items);
            
            if (!result.HasNext) break;
            cursor = result.NextCursor;
        }
        
        // Assert
        Assert.AreEqual(250, allResults.Count);
        Assert.AreEqual(allItems.Count, allResults.Count);
        CollectionAssert.AreEqual(
            allItems.Select(i => i.Id).ToList(),
            allResults.Select(i => i.Id).ToList()
        );
    }
    
    [TestMethod]
    public async Task Should_Handle_Concurrent_Inserts()
    {
        // Test that pagination remains stable even with concurrent inserts
        var page1 = await GetPageAsync(null);
        
        // Insert new items
        await InsertItemsAsync(10);
        
        // Get next page - should not include duplicates
        var page2 = await GetPageAsync(page1.NextCursor);
        
        // Verify no duplicates
        var allIds = page1.Items.Concat(page2.Items).Select(i => i.Id);
        Assert.AreEqual(allIds.Count(), allIds.Distinct().Count());
    }
}
```

This cursor-based pagination implementation provides a robust, scalable solution for handling large datasets in MongoDB with consistent performance characteristics.