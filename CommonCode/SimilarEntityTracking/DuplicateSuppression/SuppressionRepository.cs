using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Data.Entity;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
public class SuppressionRepository : MongoRepository<SuppressedItem>
{
    /// <summary>
    /// 
    /// </summary>
    protected override string CollectionName => "Suppressions";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="alerts"></param>
    public SuppressionRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) :
        base(options, alerts)
    { }
            

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected override IMongoCollection<SuppressedItem> CreateCollection()
    {
        var collection = OpenCollection();


        RunOnce.ThisCode(() =>
        {
            collection.AssureIndex(Builders<SuppressedItem>.IndexKeys.Ascending(it => it.TargetId));
            collection.AssureIndex(Builders<SuppressedItem>.IndexKeys.Ascending(it => it.ComparisonType));
            collection.AssureIndex(Builders<SuppressedItem>.IndexKeys.Ascending(it => it.ComparisonHash));
            collection.AssureIndex(Builders<SuppressedItem>.IndexKeys.Ascending(it => it.ItemId));
            collection.AssureIndex(Builders<SuppressedItem>.IndexKeys.Ascending(it => it.Version));
        });

        return collection;
    }

   

}

