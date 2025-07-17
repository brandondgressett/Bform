using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Authorization;

public class RefreshTokenRepository : MongoRepository<RefreshToken>
{
    public RefreshTokenRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "JwtRefreshTokens";

    protected override IMongoCollection<RefreshToken> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<RefreshToken>.IndexKeys.Ascending(it => it.UserId));
            collection.AssureIndex(Builders<RefreshToken>.IndexKeys.Ascending(it => it.Token));
            collection.AssureIndex(Builders<RefreshToken>.IndexKeys.Ascending(it => it.JwtId));
            collection.AssureIndex(Builders<RefreshToken>.IndexKeys.Ascending(it => it.Added));
            collection.AssureIndex(Builders<RefreshToken>.IndexKeys.Ascending(it => it.Version));
        });

        return collection;
    }
}
