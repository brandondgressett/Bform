using BFormDomain.DataModels;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPIData : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }

    public Guid KPIInstanceId { get; set; }
    public string KPITemplateName { get; set; } = null!;
        

    public DateTime SampleTime { get; set; }

    public List<KPISample> Samples { get; set; } = new();
    public List<KPISignal> Signals { get; set; } = new();
    
    
}
