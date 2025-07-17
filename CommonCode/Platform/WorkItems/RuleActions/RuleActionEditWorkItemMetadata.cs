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

public class RuleActionEditWorkItemMetadata : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionEditWorkItemMetadata(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic)
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;

    }


    public string Name => RuleUtil.FixActionName(nameof(RuleActionEditWorkItemMetadata));

    public class Arguments
    {
        
        public string? IdQuery { get; set; }

        public string? Title { get; set; }
        public string? TitleQuery { get; set; }
        public string? Description { get; set; }
        public string? DescriptionQuery { get; set; }

        public bool? IsListed { get; set; }
        public string? IsListedQuery { get; set; }
        public bool? IsVisible { get; set; }
        public string? IsVisibleQuery { get; set; }


        public string? UserAssigneeQuery { get; set; }
        public string? TriageAssigneeQuery { get; set; }

        public int? Status { get; set; }
        public string? StatusQuery { get; set; }

        public int? Priority { get; set; }
        public string? PriorityQuery { get; set; }

       

        public List<string>? SetTags { get; set; }


    }

    public async Task Execute(
        ITransactionContext trx, 
        string? result, JObject eventData, JObject? args, 
        AppEvent sourceEvent, bool sealEvents, 
        IEnumerable<string>? eventTags = null)
    {
        using(PerfTrack.Stopwatch(nameof(RuleActionEditWorkItemMetadata)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "EditedWorkItem";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;
                string resultTemplateProperty = resultProperty + "Template";

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, null);
                id.Guarantees().IsNotNull();

                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title)!;
                title.Guarantees().IsNotNullOrEmpty();

                var description = RuleUtil.MaybeLoadProp(eventData, inputs.DescriptionQuery, inputs.Description)!;
                description.Guarantees().IsNotNullOrEmpty();

                var isListed = RuleUtil.MaybeLoadProp<bool?>(eventData, inputs.IsListedQuery, inputs.IsListed);
                var isVisible = RuleUtil.MaybeLoadProp<bool?>(eventData, inputs.IsVisibleQuery, inputs.IsVisible);

                var userAssignee = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.UserAssigneeQuery, null);
                var triageAssignee = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.TriageAssigneeQuery, null);

                var status = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.StatusQuery, inputs.Status);
                var priority = RuleUtil.MaybeLoadProp<int?>(eventData, inputs.PriorityQuery, inputs.Priority);

                var origin = sourceEvent.ToPreceding(Name);

                var (instance, template) = await _logic.EventEditWorkItem(
                    origin, id!.Value, title, description, isListed, isVisible, userAssignee, triageAssignee,
                    status, priority, null, inputs.SetTags, trx!, sealEvents, eventTags);

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
