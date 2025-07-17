using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

/// <summary>
/// AcceptWorkSetInstanceContent impmelemts CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance create logic
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
public class AcceptWorkSetInstanceContent: IEntityInstanceLogic
{
    private readonly WorkSetLogic _logic;
    

    public AcceptWorkSetInstanceContent(
        WorkSetLogic logic
        )
    {
        _logic = logic;
        
    }

    public string Domain { get; } = nameof(WorkSetTemplate);

    public async Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        if(!contentInitializationMarked)
        {
            var wsCreateCommand = JsonConvert.DeserializeObject<CreateWorkSetInstanceCommand>(jsonData)!;
            wsCreateCommand.Guarantees().IsNotNull();

            await _logic.EventCreateWorkSet(new AppEvents.AppEventOrigin(nameof(AcceptContentInstance), null, null),
                wsCreateCommand.TemplateName, wsCreateCommand.Title, wsCreateCommand.Description,
                Constants.BuiltIn.SystemUser, Constants.BuiltIn.SystemUser, wsCreateCommand.Tags,
                false);
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
        Guid workSet, Guid workItem, string templateName, ProcessInstanceCommand? processInstanceCommand, JObject? creationData)
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
