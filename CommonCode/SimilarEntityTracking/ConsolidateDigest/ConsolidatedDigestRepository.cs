using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Provides peristent storage for current building digests and the items inside.
/// </summary>
public class ConsolidatedDigestRepository : MongoRepository<ConsolidatedDigest>
{
    public ConsolidatedDigestRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) :
        base(options, alerts)
    {

    }

    protected override string CollectionName => "ConsolidatedDigests";

    protected override IMongoCollection<ConsolidatedDigest> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.TargetId));
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.ComparisonType));
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.ComparisonHash));
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.Complete));
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.DigestUntil));
            collection.AssureIndex(Builders<ConsolidatedDigest>.IndexKeys.Ascending(it => it.Version));
        });

        return collection;
    }

    public Task<ConsolidatedDigest?> Load(IDigestible match)
    {
        var collection = GuardedCreateCollection();
        var candidates = collection.AsQueryable()
                                .Where(si =>
                                     si.ComparisonType == match.ComparisonType &&
                                     si.ComparisonHash == match.ComparisonHash &&
                                     si.Complete == false)
                                .ToList();

        

        ConsolidatedDigest? theMatch = null!;
        theMatch = candidates.FirstOrDefault(candidate=> candidate.ComparisonPropertyString == match.ComparisonPropertyString);
         

        return Task.FromResult(theMatch);
       
    }


}
