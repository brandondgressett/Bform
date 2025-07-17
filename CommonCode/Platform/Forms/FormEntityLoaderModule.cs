using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Forms;


/// <summary>
/// FormEntityLoaderModule implements CanLoad and LoadJson from IEntityLoaderModule to manage loading form entities from JSON
///     -Usage
///         >EntityReferenceLoader.cs
///         >WorkItemLoaderModule.cs
///     -Functions
///         >CanLoad
///         >LoadJson
/// </summary>
public class FormEntityLoaderModule : IEntityLoaderModule
{
    private readonly FormLogic _logic;


    public FormEntityLoaderModule(FormLogic logic)
    {
        _logic = logic;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(FormInstance).ToLowerInvariant() || host == nameof(FormTemplate);
    }

    public async Task<JObject?> LoadJson(string uri, string? tzid = null)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        bool wantsVM = res.Segments.Any(it => it.ToLowerInvariant() == "vm");
        bool wantsTemplate = res.Segments.Any(it => it.ToLowerInvariant() == "template");

        if (wantsTemplate)
        {
            
            if(wantsVM)
            {
                var vm = _logic.GetFormTemplateVM(res.Segments.Last());
                if (vm is not null)
                    retval = JObject.FromObject(vm);
            } else
            {
                var template = _logic.GetFormTemplate(res.Segments.Last());
                if (template is not null)
                    retval = JObject.FromObject(template);
            }
                
        } else
        {
            var id = new Guid(res.Segments.Last());

            if(wantsVM)
            {
                tzid.Requires().IsNotNull();
                var vm = await _logic.GetFormInstanceVM(id, tzid!);
                if (vm is not null)
                    retval = JObject.FromObject(vm);
            } else
            {
                var instance = await _logic.GetRawFormInstance(id);
                if (instance is not null)
                    retval = JObject.FromObject(instance);
            }

            
            
        }

        return retval;
    }
}
