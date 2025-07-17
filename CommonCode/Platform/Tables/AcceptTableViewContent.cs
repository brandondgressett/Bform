using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// AcceptTableViewContent impmelemts CanCreateWorkItemInstance,AcceptContentInstance,CreateWorkItemContent,InstancesWithAnyTags, and DetachRemoveWorkItemEntities from IEntityInstanceLogic to manage instance create logic
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
public class AcceptTableViewContent : IEntityInstanceLogic
{
    private readonly IRepository<RegisteredTableQueryWorkItemAssociation> _associations;
    
    private readonly TableLogic _logic;

    public AcceptTableViewContent(
        TableLogic logic,
       
        IRepository<RegisteredTableQueryWorkItemAssociation> associations)
    {
        _logic = logic;
       
        _associations = associations;
    }

    public string Domain => nameof(TableTemplate);

    public Task AcceptContentInstance(string jsonData, bool contentInitializationMarked)
    {
        return Task.CompletedTask;
    }

    public bool CanCreateWorkItemInstance(string name, IApplicationPlatformContent content)
    {
        bool retval = false;
        var info = content.ViewContentType(name);
        if (info is not null && info.DomainName == nameof(RegisteredTableQueryTemplate))
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
            out AppEvents.AppEventOrigin origin,
            out string? json);

        processInstanceCommand.Guarantees().IsNotNull();
        processInstanceCommand!.NamedContent.Guarantees().IsNotNull();

        var matchingQuery = content.GetContentByName<RegisteredTableQueryTemplate>(processInstanceCommand!.NamedContent!);

        int page = 0;
        if (processInstanceCommand.Vars is not null && processInstanceCommand.Vars.ContainsKey("page"))
            page = int.Parse(processInstanceCommand.Vars["page"]);

        var qp = $"query={processInstanceCommand.NamedContent}&page={page}";
        string? summary = null!;
        if (processInstanceCommand.Vars is not null && processInstanceCommand.Vars.ContainsKey("summary"))
        {

            summary = processInstanceCommand.Vars["summary"];
            qp += $"&summary={summary}";
        }

        var uri = TableEntityReferenceBuilderImplementation.MakeReference(
            templateName, Guid.Empty, true, true, qp);

        await _associations.CreateAsync(new RegisteredTableQueryWorkItemAssociation
        {
            Id = Guid.NewGuid(),
            TableTemplateName = templateName,
            RegisteredQueryTemplateName = processInstanceCommand.NamedContent!,
            RegisteredSummaryTemplateName = summary,
            Page = page,
            WorkItem = workItem,
            Uri = uri.ToString()
        });

        
        

        return uri;
    }

   

    public async Task DetachRemoveWorkItemEntities(
        AppEventOrigin? origin, Guid workItem, Uri uri, ITransactionContext? trx)
    {
        await _associations.DeleteFilterAsync(ass=>ass.WorkItem == workItem && ass.Uri == uri.ToString());
    }

   

    public async Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags,
        IApplicationPlatformContent? content=null)
    {
        content.Requires().IsNotNull();
        var allTagged = content!
            .GetMatchingAny<RegisteredTableQueryTemplate>(tags.ToArray());

        var taggedNames = allTagged
            .Select(it=>it.Name)
            .ToList();

        var (asses, _) = await _associations
            .GetAllAsync(ass => ass.WorkItem == workItem && 
                                taggedNames.Contains(ass.RegisteredQueryTemplateName));

        var retval = new List<EntitySummary>();
        foreach(var ass in asses)
        {
            var matching = allTagged.First(rtqt => rtqt.Name == ass.RegisteredQueryTemplateName);

            var qp = $"query={ass.RegisteredQueryTemplateName}&page={ass.Page}";
            string? summary = null!;
            if (!string.IsNullOrWhiteSpace(ass.RegisteredSummaryTemplateName))
            {

                summary = ass.RegisteredSummaryTemplateName;
                qp += $"&summary={summary}";
            }

            var uri = TableEntityReferenceBuilderImplementation.MakeReference(
                ass.TableTemplateName, Guid.Empty, true, true, qp);

            retval.Add(new EntitySummary
            {
                Uri = uri,
                EntityTemplate = ass.RegisteredQueryTemplateName,
                EntityTags = matching.Tags,
                EntityType = nameof(TableTemplate)
            });
        }
        return retval;


    }
}
