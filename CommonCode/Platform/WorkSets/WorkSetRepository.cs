using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetRepository : MongoRepository<WorkSet>
{
    public WorkSetRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "WorkSet";

    protected override IMongoCollection<WorkSet> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.Template));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.CreatedDate));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.UpdatedDate));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.Creator));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.LastModifier));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.HostWorkSet));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.HostWorkItem));
            collection.AssureIndex(Builders<WorkSet>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
