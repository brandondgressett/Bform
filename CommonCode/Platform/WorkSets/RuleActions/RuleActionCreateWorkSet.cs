using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets.RuleActions;

public class RuleActionCreateWorkSet : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly WorkSetLogic _logic;

    public RuleActionCreateWorkSet(IApplicationAlert alerts, WorkSetLogic logic)
    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateWorkSet));

    public class Arguments
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }

        public List<string>? InitialTags { get; set; } = new();

        public string? Title {get;set;}
        public string? TitleQuery { get; set; }
        public string? Description {get;set;}
        public string? DescriptionQuery { get; set; }

        public string? UserQuery { get; set; }
        public string? OwnerQuery { get; set; }

    }


    public async Task Execute(
        ITransactionContext trx, 
        string? result, 
        JObject eventData, 
        JObject? args, 
        AppEvent sourceEvent, 
        bool sealEvents, 
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(RuleActionCreateWorkSet)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "CreatedWorkSet";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var templateName = RuleUtil.MaybeLoadProp(eventData, inputs.TemplateNameQuery, inputs.TemplateName)!;
                templateName.Guarantees().IsNotNullOrEmpty();

                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title)!;
                title.Guarantees().IsNotNullOrEmpty();

                var description = RuleUtil.MaybeLoadProp(eventData, inputs.DescriptionQuery, inputs.Description);
                description = description ?? string.Empty;

                Guid? user = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.UserQuery, null);
                Guid? owner = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.OwnerQuery, null);

                var origin = sourceEvent.ToPreceding(Name);

                var ws = await _logic.EventCreateWorkSet(origin, templateName,
                    title, description, user, owner, inputs.InitialTags, sealEvents, trx);

                

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, JObject.FromObject(ws));

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
