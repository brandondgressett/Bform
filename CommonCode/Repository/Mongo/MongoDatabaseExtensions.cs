using MongoDB.Driver;

namespace BFormDomain.Mongo;

public static class MongoDatabaseExtensions
{
   
    public static void AssureIndex<T>(this IMongoCollection<T> that, IndexKeysDefinition<T> keys, bool unique = false)
    {
        var options = new CreateIndexOptions();
        if (unique)
            options.Unique = true;
        else
            options.Unique = false;

        var model = new CreateIndexModel<T>(keys, options);
        that.Indexes.CreateOne(model);
    }

}
