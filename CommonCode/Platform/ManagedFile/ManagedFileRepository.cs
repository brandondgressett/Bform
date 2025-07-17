using BFormDomain.Diagnostics;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;

/// <summary>
/// Repository for managed file instances.
/// </summary>
public class ManagedFileRepository : MongoRepository<ManagedFileInstance>
{
    public ManagedFileRepository(
        IOptions<MongoRepositoryOptions> options,
        SimpleApplicationAlert alerts,
        ILogger<ManagedFileRepository>? logger = null) 
        : base(options, alerts, logger)
    {
    }

    protected override string CollectionName => "ManagedFileInstances";

    protected override IMongoCollection<ManagedFileInstance> CreateCollection()
    {
        var collection = OpenCollection();
        
        // Create indexes for efficient queries
        collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(f => f.OriginalFileName));
        collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(f => f.StorageName));
        collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(f => f.CreatedDate));
        collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(f => f.Tags));
        collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(f => f.TenantId));
        
        return collection;
    }
}