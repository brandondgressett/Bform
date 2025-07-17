using BFormDomain.Diagnostics;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Repository;
using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Repository for managing Tenant entities.
/// Note: Tenants are not tenant-scoped as they represent the global tenant registry.
/// </summary>
public class TenantRepository : MongoRepository<Tenant>
{
    public TenantRepository(
        IOptions<MongoRepositoryOptions> options, 
        SimpleApplicationAlert alerts,
        ILogger<TenantRepository>? logger = null) : base(options, alerts, logger)
    {
    }

    protected override string CollectionName => "Tenants";

    protected override IMongoCollection<Tenant> CreateCollection()
    {
        var collection = OpenCollection();
        
        // Ensure indexes for efficient queries
        collection.AssureIndex(Builders<Tenant>.IndexKeys.Ascending(t => t.Name));
        collection.AssureIndex(Builders<Tenant>.IndexKeys.Ascending(t => t.IsActive));
        collection.AssureIndex(Builders<Tenant>.IndexKeys.Ascending(t => t.CreatedDate));
        collection.AssureIndex(Builders<Tenant>.IndexKeys.Ascending(t => t.Tags));
        
        return collection;
    }

    /// <summary>
    /// Get tenant by unique name
    /// </summary>
    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GuardedCreateCollection();
        var filter = Builders<Tenant>.Filter.Eq(t => t.Name, name);
        
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get all active tenants
    /// </summary>
    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        var collection = GuardedCreateCollection();
        var filter = Builders<Tenant>.Filter.Eq(t => t.IsActive, true);
        
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }
}