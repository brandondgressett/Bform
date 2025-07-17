namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPISource
{
    public string SourceName { get; set; } = null!;

    public string TableTemplate { get; set; } = null!;
    public bool UserSubject { get; set; }
    public bool WorkSetSubject { get; set; }
    public bool WorkItemSubject { get; set; }
    public string? TagSubject { get; set; }

    public int MinimumSamples { get; set; }
}
