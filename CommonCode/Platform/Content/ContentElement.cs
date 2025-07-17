using BFormDomain.CommonCode.Platform.Tags;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Metadata and boxed contents of a single content element
/// </summary>
public class ContentElement : ITaggable
{
    /// <summary>
    /// unique name for this element
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// priority among other elements, higher is more
    /// </summary>
    public int DescendingOrder { get; set; }

    /// <summary>
    /// domain it belongs to
    /// </summary>
    public string DomainName { get; set; } = "";

    /// <summary>
    /// Text of the element
    /// </summary>
    public string Serialized { get; set; } = "";

    /// <summary>
    /// Json object of the element
    /// </summary>
    public JObject? Json { get; set; }

    /// <summary>
    /// Deserialized object
    /// </summary>
    public object? Deserialized { get; set; }

    /// <summary>
    /// Tags used to group and organize content elements.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

   
}
