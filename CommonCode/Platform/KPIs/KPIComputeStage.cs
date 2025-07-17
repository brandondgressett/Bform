namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIComputeStage
{
    public string ResultName { get; set; } = null!;
    public KPIComputeType ComputeType { get; set; }
    public string? Title { get; set; }
    public List<string> ScriptLines { get; set; } = new();
    
    public int SampleId { get; set; }
}
