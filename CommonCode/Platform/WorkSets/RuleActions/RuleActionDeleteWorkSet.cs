using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets.RuleActions;

public class RuleActionDeleteWorkSet : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly WorkSetLogic _logic;

    public RuleActionDeleteWorkSet(IApplicationAlert alerts, WorkSetLogic logic)
    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionDeleteWorkSet));

    public class Arguments
    {
        public string? WorkSetIdQuery { get; set; }
        

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

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();


                Guid? id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.WorkSetIdQuery, null)!;
                id.Guarantees().IsNotNull();
                

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventDeleteWorkSet(id.Value, origin, sealEvents, trx, eventTags);


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
