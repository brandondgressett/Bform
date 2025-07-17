using BFormDomain.CommonCode.Platform.Entity;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.HtmlEntity;

/// <summary>
/// HtmlEntityLoaderModule implements CanLoad and LoadJson from IEntityLoaderModule to manage loading form entities from JSON
///     -References:
///         >EntityReferenceLoader.cs
///         >WorkItemLoaderModule.cs
///     -Functions:
///        >CanLoad
///        >LoadJson
/// </summary>
public class HtmlEntityLoaderModule : IEntityLoaderModule
{
    HtmlLogic _logic;

    public HtmlEntityLoaderModule(HtmlLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(HtmlTemplate).ToLowerInvariant();
    }

    public Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var res = new Uri(uri);
        JObject? retval = null;
        var template = _logic.GetHtml(res.Segments.Last());
        if (template is not null)
            retval = JObject.FromObject(template);
        
        return Task.FromResult(retval);
    }
}
