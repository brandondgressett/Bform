using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Comments;

public class CommentRepository : MongoRepository<Comment>
{
    public CommentRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "Comments";

    protected override IMongoCollection<Comment> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<Comment>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<Comment>.IndexKeys.Ascending(it => it.UserID));
            collection.AssureIndex(Builders<Comment>.IndexKeys.Ascending(it => it.HostEntity));
            collection.AssureIndex(Builders<Comment>.IndexKeys.Ascending(it => it.HostType));
            collection.AssureIndex(Builders<Comment>.IndexKeys.Ascending(it => it.PostDate));
        });

        return collection;
    }
}
