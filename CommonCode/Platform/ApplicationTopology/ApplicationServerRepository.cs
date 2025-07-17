using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.ApplicationTopology;

public class ApplicationServerRepository : MongoRepository<ApplicationServerRecord>
{
    public ApplicationServerRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "ApplicationTopology";

    protected override IMongoCollection<ApplicationServerRecord> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<ApplicationServerRecord>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<ApplicationServerRecord>.IndexKeys.Ascending(it => it.ServerName));
            collection.AssureIndex(Builders<ApplicationServerRecord>.IndexKeys.Ascending(it => it.LastPingTime));
            collection.AssureIndex(Builders<ApplicationServerRecord>.IndexKeys.Ascending(it => it.ServerRoles));
        });

        return collection;
    }
}
