using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.Diagnostics;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;

/// <summary>
/// AcceptKPIInstanceContent impmelemts CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance create logic
///     -References:
///         >FileApplicationPlatformContent.cs
///         >WorkItemLogic.cs
///     -Funtions:
///         >AcceptContentInstance
///         >CanCreateWorkItemInstance
///         >CreateWorkItemContent
///         >DetachRemoveWorkItemEntities
///         >InstancesWithAnyTags
/// </summary>
public class AcceptKPIInstanceContent : IEntityInstanceLogic
{

    private readonly KPILogic _logic;
    private readonly Tagger _tagger;
    private readonly IApplicationAlert _alerts;
    private readonly WorkSetAndItemFinder _finder;
  

    public AcceptKPIInstanceContent(
        KPILogic logic,
        Tagger tagger,
        WorkSetAndItemFinder finder,
        IApplicationAlert alerts)
    {
        _logic = logic;
        _tagger = tagger;
        _alerts = alerts;
        _finder = finder;
        
    }
    public string Domain => nameof(KPITemplate);

    public async Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        if(!contentInitializationMarked)
        {
            var kpiCreateCommand= JsonConvert.DeserializeObject<CreateKPIInstanceCommand>(jsonData)!;
            kpiCreateCommand.Guarantees().IsNotNull();

            await _logic.EventCreateKPI(
                new AppEvents.AppEventOrigin(nameof(AcceptContentInstance), null, null),
                kpiCreateCommand.TemplateName!,
                null, null,
                kpiCreateCommand.WorkSetHostTags,
                kpiCreateCommand.WorkItemHostTags,
                Constants.BuiltIn.SystemUser, 
                null,
                null, null,
                kpiCreateCommand.WorkSetSubjectTags,
                kpiCreateCommand.WorkItemSubjectTags,
                kpiCreateCommand.InitialTags,
                false, null);
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

    

    public async Task<Uri> CreateWorkItemContent(
        IApplicationPlatformContent content,
        Guid workSet, Guid workItem, 
        string templateName, 
        ProcessInstanceCommand? processInstanceCommand, 
        JObject? creationData)
    {
        creationData.Requires().IsNotNull();

        EntityInstanceLogicPreprocessor.ReadyInputs(
            processInstanceCommand, creationData,
            nameof(AcceptKPIInstanceContent),
            out List<string> initialTags,
            out string? namedContent,
            out AppEventOrigin origin,
            out string? json);

        var kpiCreateCommand = creationData!.ToObject<CreateKPIInstanceCommand>()!;
        kpiCreateCommand.Guarantees().IsNotNull();
        kpiCreateCommand.TemplateName = templateName;

        var id = await _logic.EventCreateKPI(
                origin,
                kpiCreateCommand.TemplateName!,
                workSet, workItem,
                kpiCreateCommand.WorkSetHostTags,
                kpiCreateCommand.WorkItemHostTags,
                Constants.BuiltIn.SystemUser,
                null,
                null, null,
                kpiCreateCommand.WorkSetSubjectTags,
                kpiCreateCommand.WorkItemSubjectTags,
                kpiCreateCommand.InitialTags,
                false, null);

        return await _logic.GetKPIInstanceRef(id);
    }



    public async Task DetachRemoveWorkItemEntities(
        AppEventOrigin? origin, Guid workItem, Uri entityUri, ITransactionContext? trx)
    {
        await _logic.EventDeleteWorkItemKPIInstance(workItem, entityUri, origin, false, trx);
    }

    
    public async Task<List<EntitySummary>> InstancesWithAnyTags(
        Guid workItem, IEnumerable<string> tags,
        
        IApplicationPlatformContent? content = null)
    {
        return await _logic.GetTaggedKPIInstanceRefs(workItem, tags);
    }
}
