using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Reports;

/// AcceptReportInstanceContent impmelemts CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance create logic
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
public class AcceptReportInstanceContent : IEntityInstanceLogic
{
    
    private readonly ReportLogic _logic;

    public AcceptReportInstanceContent(
        ReportLogic logic)
    {
        _logic = logic;
      
    }

    public string Domain => nameof(ReportTemplate);

    public Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        return Task.CompletedTask;
    }

    public bool CanCreateWorkItemInstance(string name, IApplicationPlatformContent content)
    {
        bool retval = false;
        var info = content.ViewContentType(name);
        if(info is not null && info.DomainName == Domain)
            retval = true;
        return retval;
    }

    public async Task<Uri> CreateWorkItemContent(
        IApplicationPlatformContent content,
        Guid workSet, Guid workItem, 
        string templateName, 
        ProcessInstanceCommand? processInstanceCommand, 
        JObject? creationData)
    {
        EntityInstanceLogicPreprocessor.ReadyInputs(
            processInstanceCommand, creationData,
            nameof(AcceptContentInstance),
            out List<string> initialTags,
            out string? namedContent,
            out AppEventOrigin origin,
            out string? json);

        json.Guarantees().IsNotNull();
        JObject reqData = JObject.Parse(json!);

        var id = await _logic.EventCreateReport(
            origin, templateName, workSet, workItem,
            reqData, initialTags, false, null);

        return await _logic.GetReportInstanceRef(id);

        
    }

   
    public async Task DetachRemoveWorkItemEntities(AppEventOrigin? origin, Guid workItem, Uri uri, ITransactionContext? trx)
    {
        await _logic.EventDeleteWorkItemReportInstance(workItem, uri, origin, false, trx);
    }

   

    public async Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags,
        IApplicationPlatformContent? content = null)
    {
        return await _logic.GetTaggedReportInstanceRefs(workItem, tags);
    }

}
