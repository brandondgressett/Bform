using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Forms;




/// <summary>
/// AcceptFormInstanceContent implements CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,
/// InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance create logic
///     -References:
///         >FileApplicationPlatformContent.cs
///         >WorkItemLogic.cs
///     -Functions
///         >AcceptContentInstance
///         >CanCreateWorkItemInstance
///         >CreateWorkItemContent
///         >DetachRemoveWorkItemEntities
///         >InstancesWithAnyTags
/// </summary>
public class AcceptFormInstanceContent: IEntityInstanceLogic
{
    private FormLogic? _formLogic;
    private WorkSetAndItemFinder? _finder;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _door = new object();


    public AcceptFormInstanceContent(
        IServiceProvider serviceProvider)
    {
        
        _serviceProvider = serviceProvider;
    }

    public string Domain { get; } = nameof(FormTemplate);

    /// <summary>
    /// AcceptContentInstance creates forms based on json data passed in
    /// </summary>
    /// <param name="jsonData"></param>
    /// <param name="contentInitializationMarked"></param>
    /// <returns></returns>
    private void MaybeInitialize()
    {
        lock (_door)
        {
            if(_formLogic is null || _finder is null)
            {
                _formLogic = _serviceProvider.GetService<FormLogic>();
                _finder = _serviceProvider.GetService<WorkSetAndItemFinder>();
            }    
        }
        
    }

    public async Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        MaybeInitialize();
        if(!contentInitializationMarked)
        {
            var formCreateCommand = JsonConvert.DeserializeObject<CreateFormInstancesCommand>(jsonData)!;
            formCreateCommand.Guarantees().IsNotNull();

            var wsTags = formCreateCommand.WorkSetTags;
            var wiTags = formCreateCommand.WorkItemTags;

            var (wsId, wiId) = await _finder!.Find(wsTags, wiTags);
            wsId.HasValue.Guarantees().IsTrue();
            wiId.HasValue.Guarantees().IsTrue();

            await _formLogic!.EventCreateManyForms(
                EnumerableEx.OfOne(formCreateCommand),
                new AppEvents.AppEventOrigin(nameof(AcceptContentInstance), null, null),
                wsId!.Value, wiId!.Value);

        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public bool CanCreateWorkItemInstance(string name, IApplicationPlatformContent content)
    {
        MaybeInitialize();
        bool retval = false;
        var info = content.ViewContentType(name);
        if (info is not null && info.DomainName == Domain)
            retval = true;
        return retval;
    }

    /// <summary>
    /// CreateWorkItemContent returns an instance of a form based on command passed in
    /// </summary>
    /// <param name="content"></param>
    /// <param name="workSet"></param>
    /// <param name="workItem"></param>
    /// <param name="templateName"></param>
    /// <param name="processInstanceCommand"></param>
    /// <param name="creationData"></param>
    /// <returns></returns>
    public async Task<Uri> CreateWorkItemContent(
        IApplicationPlatformContent content,
        Guid workSet, Guid workItem, string templateName, 
        ProcessInstanceCommand? processInstanceCommand, 
        JObject? creationData)
    {
        MaybeInitialize();
        EntityInstanceLogicPreprocessor.ReadyInputs(
            processInstanceCommand, creationData, 
            nameof(AcceptContentInstance),
            out List<string> initialTags, 
            out string? namedContent, 
            out AppEventOrigin origin, 
            out string? json);

        var id = await _formLogic!.EventCreateForm(origin,
            templateName, workSet, workItem,
            FormInstanceHome.AsEmbedded, initialTags,
            json, namedContent, false, null);

        return await _formLogic.GetFormInstanceRef(id);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="workItem"></param>
    /// <param name="entity"></param>
    /// <param name="trx"></param>
    /// <returns></returns>
    public async Task DetachRemoveWorkItemEntities(AppEventOrigin? origin, Guid workItem, Uri entity, ITransactionContext? trx)
    {
        MaybeInitialize();
        await _formLogic!.EventDeleteWorkItemForm(workItem, entity, origin, false, trx);
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="workItem"></param>
    /// <param name="tags"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags,
        IApplicationPlatformContent? content = null)
    {
        MaybeInitialize();
        return await _formLogic!.GetTaggedFormInstanceRefs(workItem, tags);
    }
}
