using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Forms;

public class FormInstanceRepository : MongoRepository<FormInstance>
{
    public FormInstanceRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "FormInstance";

    protected override IMongoCollection<FormInstance> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.Template));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.CreatedDate));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.UpdatedDate));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.Creator));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.LastModifier));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.HostWorkSet));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.HostWorkItem));
            collection.AssureIndex(Builders<FormInstance>.IndexKeys.Ascending(it => it.Tags));
        });

        return collection;
    }
}
