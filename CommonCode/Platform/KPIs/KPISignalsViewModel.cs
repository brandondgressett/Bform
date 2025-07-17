namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPISignalsViewModel
{
    public string Title { get; set; } = null!;

    public List<KPISignalViewModel> Data { get; set; } = new();
}
