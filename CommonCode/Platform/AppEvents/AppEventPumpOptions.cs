namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
public class AppEventPumpOptions
{
    /// <summary>
    /// CAG RE
    /// </summary>
    public int ReEnqueueTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// CAG RE
    /// </summary>
    public int RetryCutOff { get; set; } = 2;

    /// <summary>
    /// CAG RE
    /// </summary>
    public int TooAgedMinutes { get; set; } = 10;
}
