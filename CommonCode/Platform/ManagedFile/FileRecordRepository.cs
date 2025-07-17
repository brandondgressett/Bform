using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// FileRecordRepository implements IRepository as a mongo db collection for the FileRecord data model, 
/// which tracks all file metadata, and storage location information.
/// </summary>
public class FileRecordRepository : MongoRepository<ManagedFileInstance>
{
    public FileRecordRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) 
        : base(options, alerts)
    {
    }

    /// <summary>
    /// The name of the collection in the database.
    /// </summary>
    protected override string CollectionName => "ManagedFiles";

    /// <summary>
    /// Creates the managed file repository.
    /// </summary>
    /// <returns>The created collection.</returns>
    protected override IMongoCollection<ManagedFileInstance> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.OriginalFileName));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.Description));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.ContainerName));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.Tags));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.CreatedDate));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.UpdatedDate));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.LastDownload));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.GroomByDate));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.Creator));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.AttachedEntityType));
            collection.AssureIndex(Builders<ManagedFileInstance>.IndexKeys.Ascending(it => it.AttachedEntityId));

        });

        return collection;
    }
}
