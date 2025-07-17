using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// The collection that holds the history of what has happened to the files.
/// </summary>
public class ManagedFileAuditRepository : MongoRepository<ManagedFileAudit>
{
    public ManagedFileAuditRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }
    /// <summary>
    /// The name of the collection. So we don't select the wrong one.
    /// </summary>
    protected override string CollectionName => "FileAudits";
    /// <summary>
    /// Creates the file audit repository.
    /// </summary>
    /// <returns>The created collection.</returns>
    protected override IMongoCollection<ManagedFileAudit> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.OriginalFileName));
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.ManagedFileId));
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.DateTime));
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.Actor));
            collection.AssureIndex(Builders<ManagedFileAudit>.IndexKeys.Ascending(it => it.Event));
        });

        return collection;
    }
}
