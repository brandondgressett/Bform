using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems.RuleActions;

public class RuleActionCreateWorkItem : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionCreateWorkItem(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateWorkItem));

    

    public async Task Execute(
        ITransactionContext trx,
        string? result, 
        JObject eventData, 
        JObject? args, 
        AppEvent sourceEvent, 
        bool sealEvents, 
        IEnumerable<string>? eventTags = null)
    {
        using(PerfTrack.Stopwatch(nameof(RuleActionCreateWorkItem)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "CreatedWorkItem";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;
                string resultTemplateProperty = resultProperty + "Template";

                var inputs = args!.ToObject<CreateWorkItemCommand>()!;
                inputs.Guarantees().IsNotNull();

                var templateName = RuleUtil.MaybeLoadProp(eventData, inputs.TemplateNameQuery, inputs.TemplateName)!;
                templateName.Guarantees().IsNotNullOrEmpty();

                var workSet = RuleUtil.MaybeLoadProp(eventData, inputs.WorkSetQuery, sourceEvent.HostWorkSet);
                workSet.HasValue.Guarantees().IsTrue();
               
                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title)!;
                title.Guarantees().IsNotNullOrEmpty();

                var description = RuleUtil.MaybeLoadProp(eventData, inputs.DescriptionQuery, inputs.Description)!;
                description.Guarantees().IsNotNullOrEmpty();

                var isListed = RuleUtil.MaybeLoadProp<bool?>(eventData, inputs.IsListedQuery, inputs.IsListed);
                var isVisible = RuleUtil.MaybeLoadProp<bool?>(eventData, inputs.IsVisibleQuery, inputs.IsVisible);

                var userAssignee = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.UserAssigneeQuery, null);
                var triageAssignee = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.TriageAssigneeQuery, inputs.TriageAssignee);

                var status = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.StatusQuery, inputs.Status);
                var priority = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.PriorityQuery, inputs.Priority);

                var creationData = RuleUtil.MaybeLoadProp<JObject?>(eventData, inputs.CreationDataQuery, inputs.CreationData);

                var origin = sourceEvent.ToPreceding(Name);

                var (instance, template) = await _logic.EventCreateWorkItem(
                    origin, null, templateName, title, description, isListed, isVisible, userAssignee,
                    triageAssignee, status, priority, creationData, null, null, Constants.BuiltIn.SystemUser,
                    workSet!.Value, inputs.InitialTags, trx, sealEvents, eventTags);


                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, JObject.FromObject(instance));
                appendix.Add(resultTemplateProperty, JObject.FromObject(template));

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }

}
