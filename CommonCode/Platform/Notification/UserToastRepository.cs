using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Notification;

public class UserToastRepository : MongoRepository<UserToast>
{
    public UserToastRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "UserToasts";

    protected override IMongoCollection<UserToast> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<UserToast>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<UserToast>.IndexKeys.Ascending(it => it.UserId));
            collection.AssureIndex(Builders<UserToast>.IndexKeys.Ascending(it => it.Created));
        });

        return collection;
    }
}
