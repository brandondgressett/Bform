using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIDataRepository : MongoRepository<KPIData>
{
    public KPIDataRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) 
        : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(KPIData);

    protected override IMongoCollection<KPIData> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() => 
        {
            collection.AssureIndex(Builders<KPIData>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<KPIData>.IndexKeys.Ascending(it => it.KPIInstanceId));
            collection.AssureIndex(Builders<KPIData>.IndexKeys.Ascending(it => it.KPITemplateName));
            collection.AssureIndex(Builders<KPIData>.IndexKeys.Ascending(it => it.SampleTime));

        });


        return collection;
        
    }
}
