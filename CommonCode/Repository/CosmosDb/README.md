# Using Cosmos DB with BFormDomain

## Overview

Azure Cosmos DB supports a MongoDB-compatible API that allows you to use your existing MongoDB drivers, code, and tools with Cosmos DB as the backend. This means you can use the optimized `MongoRepository` implementation directly with Cosmos DB!

## Key Benefits

1. **No Code Changes**: Use the exact same `MongoRepository` class
2. **Full Compatibility**: All MongoDB optimizations work (cursor pagination, projections, aggregations)
3. **Cosmos DB Benefits**: Global distribution, guaranteed SLAs, automatic scaling
4. **Migration Path**: Easy to move between MongoDB and Cosmos DB

## Configuration

### 1. Create Cosmos DB Account with MongoDB API

```bash
# Azure CLI
az cosmosdb create \
    --name mycosmosaccount \
    --resource-group myresourcegroup \
    --kind MongoDB \
    --server-version 4.2 \
    --locations regionName=eastus
```

### 2. Get Connection String

```bash
az cosmosdb keys list \
    --name mycosmosaccount \
    --resource-group myresourcegroup \
    --type connection-strings
```

### 3. Update Configuration

```json
{
  "MongoRepositoryOptions": {
    "MongoConnectionString": "mongodb://mycosmosaccount:PRIMARY_KEY@mycosmosaccount.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@mycosmosaccount@",
    "DatabaseName": "BFormDomain",
    "UseSsl": true
  }
}
```

### 4. That's It!

Your existing `MongoRepository` code now uses Cosmos DB:

```csharp
// This code is unchanged - it just works!
public class ProductService
{
    private readonly MongoRepository<Product> _repository;
    
    public async Task<CursorPaginationResult<Product>> GetProductsAsync(
        CursorPaginationRequest request)
    {
        // All optimizations work: cursor pagination, projections, etc.
        var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);
        return await _repository.GetWithCursorAsync(request, filter);
    }
}
```

## Feature Compatibility

| MongoDB Feature | Cosmos DB MongoDB API Support | Notes |
|----------------|------------------------------|-------|
| CRUD Operations | ✅ Full | 100% compatible |
| Indexes | ✅ Full | Automatic + custom indexes |
| Aggregation Pipeline | ✅ Most stages | Some limitations on complex stages |
| Transactions | ✅ Multi-document | Within single partition |
| Change Streams | ✅ Supported | Via Cosmos DB change feed |
| GridFS | ❌ Not supported | Use Azure Blob Storage |
| Cursor Pagination | ✅ Full | Works perfectly |
| Bulk Operations | ✅ Full | Optimized for Cosmos |
| Text Search | ✅ Supported | Via Cosmos DB indexing |

## Performance Considerations

### RU/s (Request Units)
- Cosmos DB uses RU/s for billing instead of compute/storage
- Monitor RU consumption in Azure Portal
- Set appropriate throughput (min 400 RU/s)

### Partitioning
- Cosmos DB requires a partition key (shard key in MongoDB terms)
- Choose carefully - it cannot be changed later
- Common choices: `/tenantId`, `/userId`, `/category`

### Example with Partition Key:
```csharp
// In your data model
public class Product : IDataModel
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } // Partition key
    public string Name { get; set; }
    // ...
}

// MongoDB connection string includes the partition key hint
"mongodb://...?partitionKey=tenantId"
```

## Migration from MongoDB to Cosmos DB

### 1. Data Migration
```bash
# Use Azure Database Migration Service or mongoimport/mongoexport
mongodump --uri="mongodb://source-connection-string" --out=dump
mongorestore --uri="mongodb://cosmos-connection-string" dump/
```

### 2. Index Review
```javascript
// Cosmos DB creates some indexes automatically
// Review and create additional indexes as needed
db.products.createIndex({ "category": 1, "price": -1 })
```

### 3. Connection String Update
Simply update your connection string - no code changes needed!

## Cost Optimization

1. **Use Serverless**: For dev/test or variable workloads
2. **Reserved Capacity**: Save up to 65% for production
3. **Optimize Queries**: Use projections to reduce RU consumption
4. **TTL**: Set Time-to-Live for automatic cleanup

## Limitations

1. **No GridFS**: Use Azure Blob Storage for large files
2. **4MB Document Limit**: Cosmos DB limit (vs 16MB in MongoDB)
3. **Some Operators**: A few MongoDB operators not supported
4. **Transactions**: Limited to single partition

## Monitoring

```csharp
// Add diagnostics to track RU consumption
public class CosmosMongoRepository<T> : MongoRepository<T>
{
    protected override async Task<(List<T>, RepositoryContext)> GetAsync(
        int start = 0, 
        int count = 100, 
        Expression<Func<T, bool>>? predicate = null)
    {
        var sw = Stopwatch.StartNew();
        var result = await base.GetAsync(start, count, predicate);
        
        // Log RU consumption (available in response headers)
        _logger.LogInformation(
            "Query took {ElapsedMs}ms, Documents: {Count}", 
            sw.ElapsedMilliseconds, 
            result.Item1.Count);
            
        return result;
    }
}
```

## Best Practices

1. **Partition Strategy**: Design for even distribution
2. **Index Policy**: Configure based on query patterns
3. **Consistency Level**: Use Session consistency (default)
4. **Connection Pooling**: Already handled by MongoClient
5. **Retry Logic**: Already implemented in MongoRepository

## Conclusion

Using Cosmos DB's MongoDB API is the best approach for your scenario:
- **Zero code changes** to existing MongoDB repositories
- **All optimizations work** including cursor pagination
- **Full performance benefits** without rewriting
- **Easy migration** between MongoDB and Cosmos DB

Simply point your MongoDB connection string to Cosmos DB and everything works!