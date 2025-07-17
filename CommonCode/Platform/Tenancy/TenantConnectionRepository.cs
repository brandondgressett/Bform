using BFormDomain.Diagnostics;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Repository;
using BFormDomain.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Platform.Tenancy;

/// <summary>
/// Repository for managing TenantConnection entities.
/// Note: TenantConnections are not tenant-scoped as they manage the tenant connections themselves.
/// </summary>
public class TenantConnectionRepository : MongoRepository<TenantConnection>
{
    public TenantConnectionRepository(
        IOptions<MongoRepositoryOptions> options, 
        SimpleApplicationAlert alerts,
        ILogger<TenantConnectionRepository>? logger = null) : base(options, alerts, logger)
    {
    }

    protected override string CollectionName => "TenantConnections";

    protected override IMongoCollection<TenantConnection> CreateCollection()
    {
        var collection = OpenCollection();
        
        // Ensure indexes for efficient queries
        collection.AssureIndex(Builders<TenantConnection>.IndexKeys.Ascending(tc => tc.TenantId));
        collection.AssureIndex(Builders<TenantConnection>.IndexKeys.Ascending(tc => tc.Type));
        collection.AssureIndex(Builders<TenantConnection>.IndexKeys.Ascending(tc => tc.Provider));
        var compoundIndex = Builders<TenantConnection>.IndexKeys
            .Ascending(tc => tc.TenantId)
            .Ascending(tc => tc.Type);
        collection.AssureIndex(compoundIndex);
        
        return collection;
    }

    /// <summary>
    /// Get all connections for a specific tenant
    /// </summary>
    public async Task<List<TenantConnection>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var collection = GuardedCreateCollection();
        var filter = Builders<TenantConnection>.Filter.Eq(tc => tc.TenantId, tenantId);
        
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get specific connection for a tenant by type
    /// </summary>
    public async Task<TenantConnection?> GetByTenantAndTypeAsync(Guid tenantId, ConnectionType type, CancellationToken cancellationToken = default)
    {
        var collection = GuardedCreateCollection();
        var filter = Builders<TenantConnection>.Filter.And(
            Builders<TenantConnection>.Filter.Eq(tc => tc.TenantId, tenantId),
            Builders<TenantConnection>.Filter.Eq(tc => tc.Type, type)
        );
        
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}