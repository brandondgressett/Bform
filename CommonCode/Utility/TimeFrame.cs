namespace BFormDomain.CommonCode.Utility;

public class TimeFrame
{
    public int TimeFrameMinutes { get; set; }
    public int TimeFrameHours { get; set; }
    public int TimeFrameDays { get; set; }
    public int TimeFrameMonths { get; set; }

    public DateTime BackFrom(DateTime end)
    {
        return end.AddMonths(-TimeFrameMonths)
                .AddDays(-TimeFrameDays)
                .AddHours(-TimeFrameHours)
                .AddMinutes(-TimeFrameMinutes);
    }

    public DateTime From(DateTime begin)
    {
        return begin.AddMonths(TimeFrameMonths)
                .AddDays(TimeFrameDays)
                .AddHours(TimeFrameHours)
                .AddMinutes(TimeFrameMinutes);
    }
}
