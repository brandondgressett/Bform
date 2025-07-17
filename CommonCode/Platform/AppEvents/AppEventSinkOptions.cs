namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
public class AppEventSinkOptions
{
    /// <summary>
    /// CAG RE
    /// </summary>
    public int GenerationLastLimit { get; set; } = 16;
    /// <summary>
    /// CAG RE
    /// </summary>
    public int ActionTrackingExpirationSeconds { get; set; } = 60;
    /// <summary>
    /// CAG RE
    /// </summary>
    public bool DebugEvents { get; set; } = false;
}
