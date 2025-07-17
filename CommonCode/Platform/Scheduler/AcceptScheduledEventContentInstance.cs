using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
namespace BFormDomain.CommonCode.Platform.Scheduler;

/// <summary>
/// AcceptScheduledEventContentInstance implements AcceptContentInstance, CanCreateWorkItemInstance, CreateWorkItemContent, DetachRemoveWorkItemEntities, and InstancesWithAnyTags from IEntityInstanceLogic to manage 
/// deserializing json scheduled event objects.
///     -References:
///         >WorkItemLogic.cs
///     -Functions:
///         >AcceptContentInstance
///         >CanCreateWorkItemInstance
///         >CreateWorkItemContent
///         >DetachRemoveWorkItemEntities
///         >InstancesWithAnyTags
/// </summary>
public class AcceptScheduledEventContentInstance : IEntityInstanceLogic
{
    private BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _logic;
    

    public AcceptScheduledEventContentInstance(
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic logic)
    {
        _logic = logic;
        
    }

    public string Domain { get; } = nameof(ScheduledEventTemplate);

    public async Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        if (!contentInitializationMarked)
        {
            var schEvent = JsonConvert.DeserializeObject<ScheduledEvent>(jsonData)!;
            schEvent.Guarantees().IsNotNull();
            schEvent.Content.Guarantees().IsNotNull();

            await _logic.EventScheduleEventsAsync(
                schEvent.Template,
                schEvent.Content!,
                schEvent.HostWorkSet,
                schEvent.HostWorkItem,
                null,
                schEvent.Tags);
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
        throw new NotImplementedException();
    }

    public Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags,
        IApplicationPlatformContent? content = null)
    {
        throw new NotImplementedException();
    }
}
