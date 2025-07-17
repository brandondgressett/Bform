namespace BFormDomain.CommonCode.Notification;




/// <summary>
/// TimeShifts manages 3 defined Timeshifts for morning, day, and evening.
///     -References:
///         >NotificationTimeSeverityTable.cs
///         >RegulatedNotificationLogic.cs
///     -Functions:
///         >ByTime
///         >TimeUntilNextShift
/// </summary>
public class TimeShifts
{
    public static TimeShift Morning { get; private set; } = new TimeShift(0, 7);
    public static TimeShift Day { get; private set; } = new TimeShift(7, 17);
    public static TimeShift Evening { get; private set; } = new TimeShift(17, 24);


    public ChannelsAllowed DayAllowed { get; set; } = new ChannelsAllowed();
    public ChannelsAllowed EveningAllowed { get; set; } = new ChannelsAllowed();
    public ChannelsAllowed MorningAllowed { get; set; } = new ChannelsAllowed();

    public ChannelsAllowed ByTime(TimeZoneInfo tzi, DateTime utcTime = default)
    {
        if (utcTime == default)
            utcTime = DateTime.UtcNow;
        var userTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzi);
        var hourOfDay = userTime.Hour;

        ChannelsAllowed retval = MorningAllowed;
        
        switch(hourOfDay)
        {
            case int n when (n >= Morning.HourStart && n < Morning.UntilHour):
                retval = MorningAllowed;
                break;
            case int n when (n >= Day.HourStart && n < Day.UntilHour):
                retval = DayAllowed;
                break;
            case int n when n >= Evening.HourStart:
                retval = EveningAllowed;
                break;

        }

        return retval;
    }

    public static TimeSpan TimeUntilNextShift(TimeZoneInfo tzi, DateTime tm = default)
    {
        if (tm == DateTime.MinValue)
            tm = DateTime.UtcNow;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(tm, tzi);

        TimeSpan retval = TimeSpan.Zero;
        if (Morning.IsInRange(localTime.Hour))
            retval = Morning.LocalTimeUntilNextShift(localTime)!.Value;
        if (Day.IsInRange(localTime.Hour))
            retval = Day.LocalTimeUntilNextShift(localTime)!.Value;
        if(Evening.IsInRange(localTime.Hour))
            retval = Evening.LocalTimeUntilNextShift(localTime)!.Value;
        
        return retval;
    }

}
