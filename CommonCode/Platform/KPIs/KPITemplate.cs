using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPITemplate : IContentType
{
    public string Name { get; set; } = null!;
    public int DescendingOrder { get; set; } 
    public string? DomainName { get; set; } = nameof(KPITemplate);

    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public string ScheduleTemplate { get; set; } = null!;

    public string? IconClass { get; set; }

    public bool AllowUserCreate { get; set; }
    public bool AllowUserDelete { get; set; }


    public TimeFrame ComputeTimeFrame { get; set; } = new();
    public TimeFrame ViewTimeFrame { get; set; } = new();   
    
    public int SampleCount { get; set; }
    

    public List<KPISource> Sources { get; set; } = new();
    public List<KPIComputeStage> ComputeStages { get; set; } = new();
    public List<KPISignalStage> SignalStages { get; set; } = new();

    public string Title { get; set; } = null!;

    public TimeFrame? DataGroomingTimeFrame { get; set; } = null!;
}
