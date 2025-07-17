using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.KPIs.RuleActions;

public class RuleActionEvaluateKPIs : IRuleActionEvaluator
{

    private readonly IApplicationAlert _alerts;
    private readonly KPILogic _logic;

    public RuleActionEvaluateKPIs(
        IApplicationAlert alerts,
        KPILogic logic)
    {
        _alerts = alerts;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionEvaluateKPIs));

    public class Arguments
    {
        public string TopicName { get; set; } = null!;
        public DateTime? EndTime { get; set; } = null!;
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
        using (PerfTrack.Stopwatch(nameof(RuleActionEvaluateKPIs)))
        {

            try
            {
                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EvaluateMatchingKPIs(inputs.TopicName,
                    origin, sealEvents, inputs.EndTime, trx);
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }

        }
    }


}
