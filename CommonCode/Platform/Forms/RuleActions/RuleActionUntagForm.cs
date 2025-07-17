using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace BFormDomain.CommonCode.Platform.Forms.RuleActions;

public class RuleActionUntagForm : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly FormLogic _formLogic;

    public RuleActionUntagForm(
        FormLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _formLogic = logic;
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionUntagForm));

    public class Arguments
    {
        public Guid? Instance { get; set; }
        public string? InstanceQuery { get; set; }

        public List<string> Untags { get; set; } = new();
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
        using (PerfTrack.Stopwatch(nameof(RuleActionUntagForm)))
        {

            try
            {
                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var instance = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.InstanceQuery, inputs.Instance)!;
                instance.Guarantees().IsNotNull();
                var id = instance.Value;

                var origin = sourceEvent.ToPreceding(Name);

                await _formLogic.EventRemoveFormTags(
                    origin, id, inputs.Untags,
                    sealEvents, eventTags, trx);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
