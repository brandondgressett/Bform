using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BFormDomain.CommonCode.Platform.Authorization;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetMemberRepository : MongoRepository<WorkSetMember>
{
    public WorkSetMemberRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(WorkSetMember);

    protected override IMongoCollection<WorkSetMember> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<WorkSetMember>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<WorkSetMember>.IndexKeys.Ascending(it => it.WorkSetId));
            collection.AssureIndex(Builders<WorkSetMember>.IndexKeys.Ascending(it => it.UserId));

        });

        return collection;
    }
}
