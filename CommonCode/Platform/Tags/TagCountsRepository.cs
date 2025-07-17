using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Tags;

public class TagCountsRepository : MongoRepository<TagCountsDataModel>
{
    public TagCountsRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "TagCounts";

    protected override IMongoCollection<TagCountsDataModel> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<TagCountsDataModel>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<TagCountsDataModel>.IndexKeys.Ascending(it => it.Tag));
            collection.AssureIndex(Builders<TagCountsDataModel>.IndexKeys.Ascending(it => it.EntityType)); 
            collection.AssureIndex(Builders<TagCountsDataModel>.IndexKeys.Ascending(it => it.TemplateType));
            collection.AssureIndex(Builders<TagCountsDataModel>.IndexKeys.Ascending(it => it.Count));
        });

        return collection;
    }
}
