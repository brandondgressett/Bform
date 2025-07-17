using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.AppEvents;

/// <summary>
/// CAG RE
/// </summary>
public class AppEventRepository : MongoRepository<AppEvent>
{
    /// <summary>
    /// CAG RE
    /// </summary>
    /// <param name="options"></param>
    /// <param name="alerts"></param>
    public AppEventRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    /// <summary>
    /// CAG RE
    /// </summary>
    protected override string CollectionName => "AppEvents";

    /// <summary>
    /// CAG RE
    /// </summary>
    /// <returns></returns>
    protected override IMongoCollection<AppEvent> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<AppEvent>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<AppEvent>.IndexKeys.Ascending(it => it.TakenExpiration));
            collection.AssureIndex(Builders<AppEvent>.IndexKeys.Ascending(it => it.DeferredUntil));
            collection.AssureIndex(Builders<AppEvent>.IndexKeys.Ascending(it => it.State));
        });

        return collection;
    }
}
