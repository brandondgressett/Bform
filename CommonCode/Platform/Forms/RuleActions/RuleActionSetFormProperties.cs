using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Forms.RuleActions;

public class RuleActionSetFormProperties : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly FormLogic _formLogic;

    public RuleActionSetFormProperties(
        FormLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _formLogic = logic;
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionSetFormProperties));

    public class Set
    {
        public string Target { get; set; } = null!;
        public JToken? Value { get; set; }
        public string? ValueQuery { get; set; }
    }

    public class Arguments
    {
        public Guid? Instance { get; set; }
        public string? InstanceQuery { get; set; }
        public List<Set> Props { get; set; } = new();

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
        using (PerfTrack.Stopwatch(nameof(RuleActionSetFormProperties)))
        {

            try
            {
                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var instance = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.InstanceQuery, inputs.Instance)!;
                instance.Guarantees().IsNotNull();
                var id = instance.Value;

                var formData = (JObject)eventData.SelectToken("Data", true)!;

                foreach(var set in inputs.Props)
                {
                    JToken value;
                    if (set.ValueQuery is not null)
                        value = eventData.SelectToken(set.ValueQuery!, true)!;
                    else
                        value = set.Value!;
                    value.Guarantees().IsNotNull();

                    var jtProp = formData.SelectToken(set.Target);
                    if (jtProp is null)
                        formData.Add(new JProperty(set.Target, value));
                    else
                        jtProp = value;

                }

                var origin = sourceEvent.ToPreceding(Name);

                var jsonContent = formData.ToString();

                

                await _formLogic.EventUpdateFormContent(
                    origin,
                    id,
                    jsonContent,
                    eventTags,
                    sealEvents,
                    null,
                    trx);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
