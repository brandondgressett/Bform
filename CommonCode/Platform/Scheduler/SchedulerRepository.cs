using BFormDomain.CommonCode.Platform.Forms;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BFormDomain.Repository;
namespace BFormDomain.CommonCode.Platform.Scheduler
{
    public class SchedulerRepository : MongoRepository<ScheduledJobEntity>
    {
        public SchedulerRepository(IOptions<MongoRepositoryOptions> options, SimpleApplicationAlert alerts) : base(options, alerts)
        {
        }

        protected override string CollectionName => nameof(ScheduledJobEntity);

        protected override IMongoCollection<ScheduledJobEntity> CreateCollection()
        {
            var collection = OpenCollection();

            RunOnce.ThisCode(() =>
            {
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.NextDeadline));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.RecurrenceSchedule));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.Version));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.Template));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.CreatedDate));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.UpdatedDate));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.Creator));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.LastModifier));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.HostWorkSet));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.HostWorkItem));
                collection.AssureIndex(Builders<ScheduledJobEntity>.IndexKeys.Ascending(it => it.Tags));

            });

            return collection;
        }
    }
}
