using BFormDomain.CommonCode.Platform.Tags;

namespace BFormDomain.CommonCode.Platform.Content;

public interface IContentType: ITaggable
{
    string Name { get; set; }
    int DescendingOrder { get; set; }
    string? DomainName { get; set; }

    Dictionary<string, string>? SatelliteData { get; }



}
