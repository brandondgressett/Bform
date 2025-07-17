using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets.RuleActions;

public class RuleActionSetWorkSetMetadata : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly WorkSetLogic _logic;

    public RuleActionSetWorkSetMetadata(IApplicationAlert alerts, WorkSetLogic logic)
    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionSetWorkSetMetadata));

    public class Arguments
    {
        public string WorkSetIdQuery { get; set; } = null!;
        public string? Title { get; set; }
        public string? TitleQuery { get; set; }

        public string? Description { get; set; }
        public string? DescriptionQuery { get; set; }

        public string? InteractivityState { get; set; }
        
        public List<string>? SetTags { get; set; }


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

                var ws = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.WorkSetIdQuery, null)!;
                ws.Guarantees().IsNotNull();

                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title);
                var description = RuleUtil.MaybeLoadProp(eventData, inputs.DescriptionQuery, inputs.Description);
                WorkSetInteractivityState? state = null!;
                if(inputs.InteractivityState is not null)
                {
                    state = Enum.Parse<WorkSetInteractivityState>(inputs.InteractivityState);
                }

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventUpdateMetadata(origin, ws.Value, title, description, state, inputs.SetTags,
                    null, eventTags, sealEvents, trx);



            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
