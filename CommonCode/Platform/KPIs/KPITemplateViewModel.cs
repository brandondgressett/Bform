namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPITemplateViewModel
{
    public string Name { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public string? IconClass { get; set; }
    public bool AllowUserCreate { get; set; }
    public bool AllowUserDelete { get; set; }
    public string Title { get; set; } = null!;
}
