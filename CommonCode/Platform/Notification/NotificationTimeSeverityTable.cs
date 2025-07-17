using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Notification;

public class NotificationTimeSeverityTable
{
    public TimeShifts InfoSeverity { get; set; } = new TimeShifts();
    public TimeShifts WarningSeverity { get; set; } = new TimeShifts();

    public TimeShifts ErrorSeverity { get; set; } = new TimeShifts();
    public TimeShifts CriticalSeverity { get; set; } = new TimeShifts();

    public TimeShifts BySeverity(LogLevel level)
    {
        TimeShifts retval = level switch
        {
            LogLevel.Warning => WarningSeverity,
            LogLevel.Error => ErrorSeverity,
            LogLevel.Critical => CriticalSeverity,
            _ => InfoSeverity,
        };
        return retval;
    }

}
