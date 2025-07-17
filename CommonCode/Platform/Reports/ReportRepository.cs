using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Reports;

public class ReportRepository : MongoRepository<ReportInstance>
{
    public ReportRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) 
        : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(ReportInstance);

    protected override IMongoCollection<ReportInstance> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.Template));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.CreatedDate));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.Creator));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.HostWorkSet));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.HostWorkItem));
            
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.GroomDate));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.Title));
            collection.AssureIndex(Builders<ReportInstance>.IndexKeys.Ascending(it => it.Tags));

        });

        return collection;
    }
}
