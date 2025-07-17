using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIInstanceRepository : MongoRepository<KPIInstance>
{
    public KPIInstanceRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) 
        : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(KPIInstance);

    protected override IMongoCollection<KPIInstance> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.HostWorkSet));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.HostWorkItem));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.SubjectUser));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.Template));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.EventTopic));
            collection.AssureIndex(Builders<KPIInstance>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
