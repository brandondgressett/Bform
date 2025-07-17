using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using NCrontab;

namespace BFormDomain.CommonCode.Platform.Scheduler

{ 
    public class CronExpression
    {
        private CrontabSchedule _schedule;

        public CronExpression(string cronExpression)
        {
            try
            {
                _schedule = CrontabSchedule.Parse(cronExpression);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid cron expression.", ex);
            }
        }
        //Need to make a cron evaluator to spit out the next time for the cronexpression

        public TimeStatus CheckStatus(DateTime currentUtcDateTime, TimeZoneInfo timeZone, DateTime? previousInvocationTime)
        {
            DateTime currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(currentUtcDateTime, timeZone);
            DateTime? previousLocalInvocationTime = previousInvocationTime.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(previousInvocationTime.Value, timeZone)
                : (DateTime?)null;

            DateTime nextOccurrence = _schedule.GetNextOccurrence(previousLocalInvocationTime ?? DateTime.MinValue);

            if (nextOccurrence <= currentLocalDateTime)
            {
                if (previousLocalInvocationTime.HasValue && previousLocalInvocationTime.Value == nextOccurrence)
                {
                    return TimeStatus.NotDue;
                }
                return TimeStatus.Overdue;
            }

            return TimeStatus.NotDue;
        }

        public enum TimeStatus
        {
            Due,
            NotDue,
            Overdue
        }
    }
}
