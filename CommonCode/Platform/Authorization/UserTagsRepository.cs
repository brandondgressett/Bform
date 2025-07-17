using BFormDomain.Diagnostics;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.HelperClasses;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Authorization;

public class UserTagsRepository : MongoRepository<UserTagsDataModel>
{
    public UserTagsRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(UserTagsDataModel);

    protected override IMongoCollection<UserTagsDataModel> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<UserTagsDataModel>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<UserTagsDataModel>.IndexKeys.Ascending(it => it.UserName));
            collection.AssureIndex(Builders<UserTagsDataModel>.IndexKeys.Ascending(it => it.Email));
            collection.AssureIndex(Builders<UserTagsDataModel>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
