using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.KPIs.RuleActions;

public class RuleActionDeleteKPI:IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly KPILogic _logic;

    public RuleActionDeleteKPI(
        KPILogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _logic = logic;
        _alerts = alerts;
        _terms = terms;
    }
    public string Name => RuleUtil.FixActionName(nameof(RuleActionDeleteKPI));

    public class Arguments
    {
        public Guid? Id { get; set; }
        public string? IdQuery { get; set; }
        

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
        using (PerfTrack.Stopwatch(nameof(RuleActionCreateKPI)))
        {
            try
            {
                

                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                
                Guid? id = null!;
                id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, inputs.Id);
                id.HasValue.Guarantees().IsTrue();

                
                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventDeleteKPIInstance(id!.Value, origin, sealEvents, trx);
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }

    }
}
