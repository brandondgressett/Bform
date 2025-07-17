using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tables;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;

/// <summary>
/// AcceptWorkItemInstanceContent impmelemts CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance creation logic
///     -References:
///         >FileApplicationPlatformContent.cs
///         >WorkItemLogic.cs
///     -Functions:
///         >AcceptContentInstance
///         >CanCreateWorkItemInstance
///         >CreateWorkItemContent
///         >DetachRemoveWorkItemEntities
///         >InstancesWithAnyTags
/// </summary>
public class AcceptWorkItemInstanceContent : IEntityInstanceLogic
{
    
    private readonly WorkItemLogic _logic;
    private readonly IRepository<WorkSet> _workSets;
    

    public AcceptWorkItemInstanceContent(
        WorkItemLogic logic,
    
        IRepository<WorkSet> workSets)
    {
        _logic = logic;
    
        _workSets = workSets;
    }

    public string Domain => nameof(WorkItem);

    public async Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        if(!contentInitializationMarked)
        {
            var wsCreateCommand = JsonConvert.DeserializeObject<CreateWorkItemCommand>(jsonData)!;
            wsCreateCommand.Guarantees().IsNotNull();

            var tags = wsCreateCommand.WorkSetTagged!;
            tags.Guarantees().IsNotNull();
            tags.Guarantees().IsNotEmpty();

            var (ws, _) = await _workSets.GetOneAsync(ws => tags.All(tg => ws.Tags.Contains(tg)));
            ws.Guarantees().IsNotNull();

            await _logic.EventCreateWorkItem(new AppEventOrigin(nameof(AcceptWorkItemInstanceContent), null, null),
                null, wsCreateCommand.TemplateName!, wsCreateCommand.Title!, wsCreateCommand.Description,
                wsCreateCommand.IsListed, wsCreateCommand.IsVisible, null, wsCreateCommand.TriageAssignee,
                wsCreateCommand.Status, wsCreateCommand.Priority, wsCreateCommand.CreationData,
                null, null, Constants.BuiltIn.SystemUser, ws!.Id, wsCreateCommand.InitialTags,
                null, false, null);

           
        }
    }

    public bool CanCreateWorkItemInstance(string name, IApplicationPlatformContent content)
    {
        bool retval = false;
        var info = content.ViewContentType(name);
        if (info is not null && info.DomainName == Domain)
            retval = true;
        return retval;
    }

    public Task<Uri> CreateWorkItemContent(
        IApplicationPlatformContent content,
        Guid workSet, Guid workItem, 
        string templateName, 
        ProcessInstanceCommand? processInstanceCommand, 
        JObject? creationData)
    {
       

        throw new NotImplementedException();
    }

    public Task DetachRemoveWorkItemEntities(AppEventOrigin? origin, Guid workItem, Uri uri, ITransactionContext? trx)
    {
        return Task.CompletedTask;
    }

    public Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags,
        IApplicationPlatformContent? content = null)
    {
        
        throw new NotImplementedException();
    }
}
