using BFormDomain.HelperClasses;

namespace BFormDomain.CommonCode.Utility;

public enum DateTruncation
{
    Year,
    Quarter,
    Month,
    Week,
    Day,
    Hour,
    Second
}

public static class DateTimeUtility
{
    public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var retval = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        retval = retval.AddSeconds(unixTimeStamp);
        return retval;
    }

    public static DateTime Truncate(this DateTime dt, DateTruncation t)
    {
        switch(t)
        {
            case DateTruncation.Year: dt = dt.TruncateToYearStart(); break;
            case DateTruncation.Quarter: dt = dt.TruncateToQuarterlyStart(); break;
            case DateTruncation.Month: dt = dt.TruncateToMonthStart(); break;
            case DateTruncation.Week: dt = dt.TruncateToWeekStart(); break;
            case DateTruncation.Day: dt = dt.TruncateToDayStart(); break;
            case DateTruncation.Hour: dt = dt.TruncateToHourStart(); break;
            case DateTruncation.Second: dt = dt.TruncateToSecondStart(); break;
        }
        return dt;
    }

    public static DateTime TruncateToYearStart(this DateTime dt)
    {
        return new DateTime(dt.Year, 1, 1);
    }

    public static DateTime TruncateToQuarterlyStart(this DateTime dt)
    {
        var tc = new TemporalCollocator();
        tc.CollocateTime(dt);
        return tc.LastDayPrevQuarter.AddDays(1.0);
    }

    public static DateTime TruncateToMonthStart(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, 1);
    }

    public static DateTime TruncateToWeekStart(this DateTime dt)
    {
        var tc = new TemporalCollocator();
        tc.CollocateTime(dt);
        return tc.StartOfWeek;
    }

    public static DateTime TruncateToDayStart(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day);
    }

    public static DateTime TruncateToHourStart(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
    }

    public static DateTime TruncateToMinuteStart(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
    }

    public static DateTime TruncateToSecondStart(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
    }
}
