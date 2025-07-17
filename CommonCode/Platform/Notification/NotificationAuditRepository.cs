using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Notification;

public class NotificationAuditRepository : MongoRepository<NotificationAudit>
{

    protected override string CollectionName => "NotificationAudit";

    public NotificationAuditRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }
    

    protected override IMongoCollection<NotificationAudit> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<NotificationAudit>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<NotificationAudit>.IndexKeys.Ascending(it => it.UserRef));
            collection.AssureIndex(Builders<NotificationAudit>.IndexKeys.Ascending(it => it.Created));

        });

        return collection;
    }
}
