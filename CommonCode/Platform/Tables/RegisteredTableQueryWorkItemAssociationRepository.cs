using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Tables;

public class RegisteredTableQueryWorkItemAssociationRepository : MongoRepository<RegisteredTableQueryWorkItemAssociation>
{
    public RegisteredTableQueryWorkItemAssociationRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => nameof(RegisteredTableQueryWorkItemAssociation);

    protected override IMongoCollection<RegisteredTableQueryWorkItemAssociation> CreateCollection()
    {
        var collection = OpenCollection();

        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<RegisteredTableQueryWorkItemAssociation>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<RegisteredTableQueryWorkItemAssociation>.IndexKeys.Ascending(it => it.WorkItem));
            collection.AssureIndex(Builders<RegisteredTableQueryWorkItemAssociation>.IndexKeys.Ascending(it => it.TableTemplateName));
            collection.AssureIndex(Builders<RegisteredTableQueryWorkItemAssociation>.IndexKeys.Ascending(it => it.RegisteredQueryTemplateName));
            collection.AssureIndex(Builders<RegisteredTableQueryWorkItemAssociation>.IndexKeys.Ascending(it => it.Uri));
        });

        return collection;
    }
}
