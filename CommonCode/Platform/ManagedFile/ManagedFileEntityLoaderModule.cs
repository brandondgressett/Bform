using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.ManagedFile;

public class ManagedFileEntityLoaderModule : IEntityLoaderModule
{
    private readonly ManagedFileLogic _logic;


    public ManagedFileEntityLoaderModule(ManagedFileLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(ManagedFileInstance).ToLowerInvariant();
    }

    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        bool wantsVM = res.Segments.Any(it => it.ToLowerInvariant() == "vm");
        var id = new Guid(res.Segments.Last());

        if (wantsVM)
        {
            var vm = await _logic.GetFileVMAsync(id, tzid!);
            if(vm is not null)
                retval = JObject.FromObject(vm);
        } else
        {
            var instance = await _logic.GetFileAsync(id);
            if(instance is not null)
                retval= JObject.FromObject(instance);
        }


        return retval;
    }
}
