using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Notification;

public class NotificationContactRepository : MongoRepository<NotificationContact>
{
    public NotificationContactRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "NotificationContact";

    protected override IMongoCollection<NotificationContact> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.UserRef));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.EmailAddress));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.TextNumber));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.CallNumber));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.ContactTitle));
            collection.AssureIndex(Builders<NotificationContact>.IndexKeys.Ascending(it => it.Active));

        });

        return collection;
    }


}
