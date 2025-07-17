using BFormDomain.CommonCode.Platform.Entity;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.KPIs;

/// <summary>
/// KPIEntityLoaderModule implements CanLoad and LoadJson from IEntityLoaderModule to load KPI instances fron JSON
///     -References:
///         >EntityReferenceLoader.cs
///         >WorkItemLoaderModule.cs
///     -Funtions:
///         >CanLoad
///         >LoadJson
/// </summary>
public class KPIEntityLoaderModule : IEntityLoaderModule
{
    private readonly KPILogic _logic;

    public KPIEntityLoaderModule(KPILogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(KPIInstance).ToLowerInvariant();
    }

    public async Task<JObject?> LoadJson(string uri, string? tzid =null)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        bool wantsVM = res.Segments.Any(it => it.ToLowerInvariant() == "vm");
           
        
        bool wantsTemplate = res.Segments.Any(it => it.ToLowerInvariant() == "template");

        if(wantsTemplate)
        {
            var templateName = res.Segments.Last();
            if (wantsVM)
            {
                var vm = _logic.GetKPITemplateVM(templateName);
                if(vm is not null)
                    retval = JObject.FromObject(vm);
            } else
            {
                var template = _logic.GetRawKPITemplate(templateName);
                if(template is not null)
                    retval = JObject.FromObject(template);
            }
        } else
        {
            var id = new Guid(res.Segments.Last());
            if(wantsVM)
            {
                var vm = await _logic.GetKPIInstanceVM(id, tzid, null, null);
                if (vm is not null)
                    retval = JObject.FromObject(vm);
            } else
            {
                var instance = await _logic.GetRawKPIInstance(id);
                if(instance is not null)
                    retval= JObject.FromObject(instance);
            }
        }


        return retval;
    }
}
