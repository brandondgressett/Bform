using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Tables;

public class TableMetadataRepository : MongoRepository<TableMetadata>
{
    public TableMetadataRepository(
        IOptions<MongoRepositoryOptions> options,
        SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(TableMetadata);

    protected override IMongoCollection<TableMetadata> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.TemplateName));

            var options = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<TableMetadata>
                (Builders<TableMetadata>.IndexKeys.Ascending(it => it.CollectionName), new CreateIndexOptions { Unique = true });
            collection.Indexes.CreateOne(indexModel);
                        
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.PerWorkSet));
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.PerWorkItem));
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.NeedsGrooming));
            collection.AssureIndex(Builders<TableMetadata>.IndexKeys.Ascending(it => it.LastGrooming));
        });

        return collection;
    }
}
