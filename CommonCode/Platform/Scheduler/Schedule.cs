using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Utility;
using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Platform.Scheduler
{

    /*
    public class Schedule : IDataModel {
        public Guid Id { get; set; }
        public int Version { get; set; }

        public DateTime? NextDeadline { get; set; }
                
        public TimeSpan RecurrenceSchedule { get; set; }

        public enum ScheduleType
        {
            Once,
            RecurringX,
            RecurringInfinite,
            Cron
        }   

        public string? CronExpression { get; set; }

        public ScheduleType Type { get; set; }
        public int RecurrenceCount { get; set; }
        public int InvocationCount { get; set; }

        public bool RepeatForever { get; set; } = false;

        [JsonConverter(typeof(BsonToJsonConverter))]
        public BsonDocument? Content { get; set; }

        [BsonIgnore]
        [JsonIgnore]
        public JObject JobContent { get { return JObject.Parse(Content!.ToJsonString()); } set { Content = value.ToBsonObject(); } }


        public ScheduledEventIdentifier? ScheduledEventID { get; set; }

        public Guid Id { get; set; }
        public int Version { get; set; }
        
    }
    */
}
