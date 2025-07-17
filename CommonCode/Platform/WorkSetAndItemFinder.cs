using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Repository;

namespace BFormDomain.CommonCode.Platform;

public class WorkSetAndItemFinder
{
    private readonly Tagger _tagger;
    private readonly IRepository<WorkSet> _workSets;
    private readonly IRepository<WorkItem> _workItems;

    public WorkSetAndItemFinder(
        Tagger tagger, 
        IRepository<WorkSet> workSets,
        IRepository<WorkItem> workItems)
    {
        _tagger= tagger;
        _workSets= workSets;
        _workItems= workItems;
    }


    public async Task<(Guid?,Guid?)> Find(IEnumerable<string>? workSetTags, IEnumerable<string>? workItemTags)
    {
        Guid? workSet = null!;
        Guid? workItem = null!;

        if(workSetTags is not null && workSetTags.Any())
            workSet = (await _tagger.IdsFromTags(workSetTags, _workSets)).Single();
        if (workItemTags is not null && workItemTags.Any())
        {
            if(workSet is not null)
                workItem = (await _tagger.IdsFromTags(workItemTags, _workItems, workSet)).Single();
            else
                workItem = (await _tagger.IdsFromTags(workItemTags, _workItems, null)).Single();
        }

        return (workSet, workItem);
    }

}
