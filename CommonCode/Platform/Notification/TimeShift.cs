namespace BFormDomain.CommonCode.Notification;

/// <summary>
/// TimeShift manages information about individual durations of time assigned as a shift
///     -References:
///         >TimeShitfs.cs
///     -Functions:
///         >IsInRange
///         >TimeUntilNextShift
///         >LocalTimeUntilNextShift
/// </summary>
public class TimeShift
{
    public TimeShift(int st, int until)
    {
        HourStart = st;
        UntilHour = until;
    }

    public int HourStart { get; set; }
    public int UntilHour { get; set; }
    public bool IsInRange(int hour)
    {
        return hour >= HourStart && hour < UntilHour;
    }

    public TimeSpan? TimeUntilNextShift(TimeZoneInfo tzi, DateTime tm = default)
    {
        if (tm == DateTime.MinValue)
            tm = DateTime.UtcNow;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(tm, tzi);
        return LocalTimeUntilNextShift(localTime);
    }

    public TimeSpan? LocalTimeUntilNextShift(DateTime localTime)
    {
        if (!IsInRange(localTime.Hour))
            return null;

        var nextDt = new DateTime(
            localTime.Year,
            localTime.Month,
            localTime.Day,
            UntilHour,
            localTime.Minute,
            localTime.Second);

        var diff = nextDt - localTime;
        return diff;
    }
}
