using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemRepository : MongoRepository<WorkItem>
{
    public WorkItemRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "WorkItem";

    protected override IMongoCollection<WorkItem> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.Template));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.CreatedDate));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.UpdatedDate));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.Creator));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.LastModifier));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.HostWorkSet));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.HostWorkItem));
            collection.AssureIndex(Builders<WorkItem>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
