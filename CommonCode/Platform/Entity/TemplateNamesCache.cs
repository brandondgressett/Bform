using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.CommonCode.Utility.Caching;
using BFormDomain.Repository;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.ManagedFiles;

using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
namespace BFormDomain.CommonCode.Platform.Entity;

// should be a singleton.
/// <summary>
/// TemplateNamesCache gets cached templates by work item/work set
///     References:
///         >CommentsLogic.cs
///         >FormLogic.cs
///         >KPILogic.cs
///         >ManagedFileLogic.cs
///         >ReportLogic.cs
///         >QuartzISchedulerLogic.cs
///         >TableLogic.cs
///         >WorkItemLogic.cs
///         >WorksetLogic.cs
///     -Functions:
///         >GetWorkSetTemplateName
///         >GetTemplateNames
/// </summary>
public class TemplateNamesCache
{
    private readonly ICachedData<Guid, string> _templateNamesCache;
    private readonly IRepository<WorkItem> _workItems;
    private readonly IRepository<WorkSet> _workSets;


    public TemplateNamesCache(
        IRepository<WorkSet> workSets,
        IRepository<WorkItem> workItems)
    {
        _workSets = workSets;
        _workItems = workItems; 

        _templateNamesCache =
            new InMemoryCachedData<Guid, string>(
                expireItems: true,
                renewOnCacheHit: true,
                defaultExpireTime: TimeSpan.FromSeconds(600),
                maximumCacheItemsCount: 16384);
    }



    public async Task<(string, string)> GetTemplateNames(Guid workSetId, Guid workItemId)
    {
        string? wsTemplate, wiTemplate;
        if (workSetId == Constants.BuiltIn.SystemWorkSet)
            wsTemplate = "System.BForm";
        else if (!_templateNamesCache.MaybeGetItem(workSetId, out wsTemplate))
        {
            var (ws, _) = await _workSets.LoadAsync(workSetId);
            ws.Guarantees().IsNotNull();
            wsTemplate = ws.Template;
            _templateNamesCache.Add(workSetId, wsTemplate);
        }

        if(workItemId == Constants.BuiltIn.SystemWorkItem)
        {
            wiTemplate = "System.BForm";
        } else if (!_templateNamesCache.MaybeGetItem(workItemId, out wiTemplate))
        {
            var (wi, _) = await _workItems.LoadAsync(workItemId);
            wi.Guarantees().IsNotNull();
            wiTemplate = wi.Template;
            _templateNamesCache.Add(workItemId, wiTemplate);
        }

        return (wsTemplate!, wiTemplate!);
    }

    public async Task<string> GetWorkSetTemplateName(Guid workSetId)
    {
        string? wsTemplate;
        if (workSetId == Constants.BuiltIn.SystemWorkSet)
            wsTemplate = "System.BForm";
        else
        if (!_templateNamesCache.MaybeGetItem(workSetId, out wsTemplate))
        {
            var (ws, _) = await _workSets.LoadAsync(workSetId);
            ws.Guarantees().IsNotNull();
            wsTemplate = ws.Template;
            _templateNamesCache.Add(workSetId, wsTemplate);
        }
               

        return (wsTemplate!);
    }
}
