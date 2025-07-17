using BFormDomain.CommonCode.Utility;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BFormDomain.CommonCode.Platform.Entity;

namespace BFormDomain.CommonCode.Platform.Scheduler
{
    public enum ScheduleType
    {
        Once,
        RecurringX,
        RecurringInfinite,
        Cron
    }

    public class ScheduledJobData
    {

        public const string TopicTemplate = "scheduler.job.invoked.{0}";
        public string? Name { get; set; }
        public string? Topic
        {
            get { return string.Format(TopicTemplate, Name); }
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

      

    }

    public class ScheduledJobEntity: EntityWrapping<ScheduledJobData>
    {

        public DateTime? NextDeadline { get; set; }

        public TimeSpan RecurrenceSchedule { get; set; }


    }



}
