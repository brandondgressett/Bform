using BFormDomain.CommonCode.Platform.Entity;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemLoaderModule : IEntityLoaderModule
{

    private readonly WorkItemLogic _logic;

    public WorkItemLoaderModule(WorkItemLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(WorkItem).ToLowerInvariant();
    }
      

    Task<JObject?> IEntityLoaderModule.LoadJson(string uri, string? tzid)
    {
        throw new NotImplementedException();
    }
}
