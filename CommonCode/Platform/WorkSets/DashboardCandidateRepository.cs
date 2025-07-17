using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.WorkSets;

public class DashboardCandidateRepository : MongoRepository<DashboardCandidate>
{
    public DashboardCandidateRepository(
        IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(DashboardCandidate);

    protected override IMongoCollection<DashboardCandidate> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.WorkSet));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.Score));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.Grouping));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.Created));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.IsWinner));
            collection.AssureIndex(Builders<DashboardCandidate>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
