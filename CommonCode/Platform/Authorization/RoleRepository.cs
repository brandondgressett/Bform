using BFormDomain.CommonCode.Authorization;
using BFormDomain.Diagnostics;
using BFormDomain.Mongo;
using BFormDomain.Repository;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Authorization
{
    public class RoleRepository : MongoRepository<ApplicationRole>
    {
        public RoleRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
        {
        }

        protected override string CollectionName => nameof(ApplicationRole);

        protected override IMongoCollection<ApplicationRole> CreateCollection()
        {
            var collection = OpenCollection();

            collection.AssureIndex(Builders<ApplicationRole>.IndexKeys.Ascending(it => it.Claims));
            collection.AssureIndex(Builders<ApplicationRole>.IndexKeys.Ascending(it => it.ConcurrencyStamp));
            collection.AssureIndex(Builders<ApplicationRole>.IndexKeys.Ascending(it => it.Name));
            collection.AssureIndex(Builders<ApplicationRole>.IndexKeys.Ascending(it => it.NormalizedName));
            
            return collection;
        }
    }
}
