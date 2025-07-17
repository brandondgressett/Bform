using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Entity;

public interface IEntityLoaderModule
{
    bool CanLoad(string uri);
    Task<JObject?> LoadJson(string uri, string? tzid = null);
}
