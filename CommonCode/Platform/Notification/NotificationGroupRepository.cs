using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Notification;

public class NotificationGroupRepository : MongoRepository<NotificationGroup>
{
    public NotificationGroupRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "NotificationGroup";

    protected override IMongoCollection<NotificationGroup> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<NotificationGroup>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<NotificationGroup>.IndexKeys.Ascending(it => it.GroupTitle));
            collection.AssureIndex(Builders<NotificationGroup>.IndexKeys.Ascending(it => it.Tags));
            collection.AssureIndex(Builders<NotificationGroup>.IndexKeys.Ascending(it => it.Active));

        });

        return collection;
    }
}
