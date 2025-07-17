using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;

public class HtmlTemplate : IContentType
{
    public string Name { get; set; } = nameof(HtmlTemplate);
    public int DescendingOrder { get; set; }
    public string? DomainName { get; set; }

    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public string Content { get; set; } = null!;

}
