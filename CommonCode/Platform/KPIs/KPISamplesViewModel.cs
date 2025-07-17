namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPISamplesViewModel
{
    public string Title { get; set; } = null!;
    public bool IsMain { get; set; }

    public List<KPISampleViewModel> Data { get; set; } = new();
}
