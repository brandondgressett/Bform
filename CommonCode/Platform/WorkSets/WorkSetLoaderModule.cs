using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Repository;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets;

/// <summary>
/// WorkSetLoaderModule implements CanLoad and LoadJson from IEntityLoaderModule to manage loading work set entities from JSON
///     -Usage
///         >EntityReferenceLoader.cs
///         >WorkItemLoaderModule.cs
///     -Functions
///         >CanLoad
///         >LoadJson
/// </summary>
public class WorkSetLoaderModule : IEntityLoaderModule
{
    private readonly WorkSetLogic _logic;
    private readonly IRepository<WorkSet> _workSets;
    private readonly IApplicationPlatformContent _content;

    public WorkSetLoaderModule(WorkSetLogic logic,
        IRepository<WorkSet> workSets, 
        IApplicationPlatformContent content)
    {
        _logic = logic;
        _workSets = workSets;
        _content = content;
    }

    public bool CanLoad(string uri)
    {
        var res = new Uri(uri);
        var host = res.Host.ToLowerInvariant();
        return host == nameof(WorkSet).ToLowerInvariant();
    }


   
    public async Task<JObject?> LoadJson(string uri, string? tzid)
    {
        var res = new Uri(uri);
        JObject? retval = null!;
        bool wantsVM = res.Segments.Any(it => it.ToLowerInvariant() == "vm");
        bool wantsTemplate = res.Segments.Any(it => it.ToLowerInvariant() == "template");

        if(wantsTemplate)
        {
            var name = res.Segments.Last();
            var template = _content.GetContentByName<WorkSetTemplate>(name);
            if (template is not null)
            {
                if (wantsVM)
                {
                    var vm = WorkSetTemplateViewModel.Create(template);
                    retval= JObject.FromObject(vm);

                }
                else
                {
                    retval = JObject.FromObject(template);
                }
            }
            
        } else
        {
            var id = new Guid(res.Segments.Last());

            if (wantsVM)
            {
                var vm = await _logic.GetWorkSetViewModel(id);
                if(vm is not null)
                    retval = JObject.FromObject(vm);

            } else
            {
                var (instance,_) = await _workSets.LoadAsync(id);
                if(instance is not null)
                    retval= JObject.FromObject(instance);
            }
        }

        return retval;
    }
}
