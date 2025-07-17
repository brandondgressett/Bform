using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Authorization;

public class InvitationRepository : MongoRepository<InvitationDataModel>
{
    public InvitationRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
    {
    }

    protected override string CollectionName => "Invitations";

    protected override IMongoCollection<InvitationDataModel> CreateCollection()
    {
        var collection = OpenCollection();


        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<InvitationDataModel>.IndexKeys.Ascending(it => it.Version));
            collection.AssureIndex(Builders<InvitationDataModel>.IndexKeys.Ascending(it => it.EmailTarget));
            collection.AssureIndex(Builders<InvitationDataModel>.IndexKeys.Ascending(it => it.Expiration));
            collection.AssureIndex(Builders<InvitationDataModel>.IndexKeys.Ascending(it => it.InvitationCode));

        });

        return collection;
    }
}
