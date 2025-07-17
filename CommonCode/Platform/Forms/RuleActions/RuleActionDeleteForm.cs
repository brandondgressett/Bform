using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Forms.RuleActions;

public class RuleActionDeleteForm : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly FormLogic _formLogic;

    public RuleActionDeleteForm(
        FormLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _formLogic = logic;
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionDeleteForm));

    public class Arguments
    {
        public Guid? InstanceId { get; set; }
        public string? InstanceIdQuery { get; set; }
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
        using(PerfTrack.Stopwatch(nameof(RuleActionDeleteForm)))
        {
            try
            {
                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.InstanceIdQuery, inputs.InstanceId);
                id.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);
                await _formLogic.EventDeleteForm(id!.Value, origin, sealEvents, trx);


            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
