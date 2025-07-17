using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets.RuleActions;

public class RuleActionWorkSetRemoveMember : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly WorkSetLogic _logic;

    public RuleActionWorkSetRemoveMember(IApplicationAlert alerts, WorkSetLogic logic)
    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionWorkSetRemoveMember));

    public class Arguments
    {
        public string? WorkSetIdQuery { get; set; }
        public string? MemberIdQuery { get; set; }

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

                Guid? workSet = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.WorkSetIdQuery, null)!;
                Guid? member = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.MemberIdQuery, null)!;
                workSet.Guarantees().IsNotNull();
                member.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventRemoveMember(origin, workSet.Value, member.Value, null, eventTags, sealEvents, trx);


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
