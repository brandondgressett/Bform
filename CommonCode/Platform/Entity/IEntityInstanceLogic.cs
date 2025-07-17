using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Entity;


public class JSub
{
    public string TargetJsonPath { get; set; } = null!;
    public string? SourceJsonPath { get; set; }
    public JToken? SourceJsonValue { get; set; }
}

/// <summary>
/// 
///     References:
///         >
///     -Functions:
///         >
/// </summary>
public class ProcessInstanceCommand
{
    public string? NamedContent { get; set; }

    public Dictionary<string,string>? Vars { get; set; }

    public List<string>? InitialTags { get; set; }
    public JsonWinnower? PreProcess { get; set; }
    public List<JSub>? Substitutions { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public JObject ProcessSubs(JObject inputs)
    {
        if(PreProcess is not null)
            PreProcess.WinnowData(inputs);

        foreach(var sub in Substitutions.EmptyIfNull())
        {
            var target = inputs.SelectToken(sub.TargetJsonPath);
            if (target is null)
                continue;

            var property = (JProperty) target.Parent!;

            if (sub.SourceJsonPath is not null )
            {
                var sourceValue = inputs.SelectToken(sub.SourceJsonPath);
                if (sourceValue is not null)
                    property.Value = sourceValue;
            } else
            {
                sub.SourceJsonValue.Requires().IsNotNull();
                property.Value = sub.SourceJsonValue!;
            }
        }

        return inputs;

    }
    
}

public class EntitySummary
{
    public string EntityTemplate { get; set; } = null!;
    public Uri Uri { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public List<string> EntityTags { get; set; } = new();
    
}

/// <summary>
/// IEntityInstanceLogic creates work item content
///     References:
///         >FileApplicationPlatformContent.cs
///         >AcceptFormInstanceContent.cs
///         >AcceptKPIInstanceContent.cs
///         >AcceptReportInstanceContent.cs
///         >AcceptScheduledEventContentInstance.cs
///         >AcceptTableViewContent.cs
///         >AcceptWorkItemInstanceContent.cs
///         >WorkItemLogic.cs
///         >AcceptWorkSetInstanceContent.cs
///     -Functions:
///         >CreateWorkItemContent
/// </summary>
public interface IEntityInstanceLogic
{
    string Domain { get; }

    bool CanCreateWorkItemInstance(string name, IApplicationPlatformContent content);
    Task AcceptContentInstance(string jsonData, bool contentInitializationMarked);

    /// <summary>
    /// CreateWorkItemContent creates work item content
    /// </summary>
    /// <param name="content"></param>
    /// <param name="workSet"></param>
    /// <param name="workItem"></param>
    /// <param name="templateName"></param>
    /// <param name="processInstanceCommand"></param>
    /// <param name="creationData"></param>
    /// <returns></returns>
    Task<Uri> CreateWorkItemContent(
        IApplicationPlatformContent content,
        Guid workSet,
        Guid workItem,
        string templateName,
        ProcessInstanceCommand? processInstanceCommand,
        JObject? creationData);

    Task<List<EntitySummary>> InstancesWithAnyTags(Guid workItem, IEnumerable<string> tags, 
        IApplicationPlatformContent? content = null);

    Task DetachRemoveWorkItemEntities(AppEventOrigin? origin, Guid workItem, Uri entity, ITransactionContext? trx);
}

/// <summary>
/// 
///     References:
///         >
///     -Functions:
///         >
/// </summary>
public static class EntityInstanceLogicPreprocessor
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInstanceCommand"></param>
    /// <param name="creationData"></param>
    /// <param name="originName"></param>
    /// <param name="initialTags"></param>
    /// <param name="namedContent"></param>
    /// <param name="origin"></param>
    /// <param name="json"></param>
    public static void ReadyInputs(
       ProcessInstanceCommand? processInstanceCommand,
       JObject? creationData,
       string originName,
       out List<string> initialTags,
       out string? namedContent,
       out AppEventOrigin origin,
       out string? json)
    {
        JObject? inputs = creationData;
        initialTags = new();
        namedContent = null!;
        if (processInstanceCommand is not null && inputs is not null)
        {
            if (processInstanceCommand.PreProcess is not null)
            {
                processInstanceCommand.PreProcess.WinnowData(inputs);
            }

            if (processInstanceCommand.Substitutions.EmptyIfNull().Any())
                inputs = processInstanceCommand.ProcessSubs(inputs);

            if (processInstanceCommand.InitialTags is not null)
                initialTags = processInstanceCommand.InitialTags;

            namedContent = processInstanceCommand.NamedContent;
        }

        origin = new AppEvents.AppEventOrigin(originName, null, null);
        json = null!;
        if (inputs is not null)
            json = inputs.ToString();
    }
}