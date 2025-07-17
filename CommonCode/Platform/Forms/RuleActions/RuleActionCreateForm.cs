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

public class RuleActionCreateForm : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly FormLogic _formLogic;

    public RuleActionCreateForm(
        FormLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _formLogic = logic;
        _alerts = alerts;
        _terms = terms;
    }
    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateForm));

    public class Arguments
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }

        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }

        public List<string>? InitialTags { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FormInstanceHome Home { get; set; }

        public string? InitName { get; set; }
        public string? InitNameQuery { get; set; }

        public JObject? InitialProps { get; set; }
        public string? InitialPropsQuery { get; set; }
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
        using(PerfTrack.Stopwatch(nameof(RuleActionCreateForm)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "CreatedForm";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var templateName = RuleUtil.MaybeLoadProp(eventData, inputs.TemplateNameQuery, inputs.TemplateName)!;
                templateName.Guarantees().IsNotNullOrEmpty();
                var workSet = RuleUtil.MaybeLoadProp(eventData, inputs.WorkSetQuery, sourceEvent.HostWorkSet);
                var workItem = RuleUtil.MaybeLoadProp(eventData, inputs.WorkItemQuery, sourceEvent.HostWorkItem);
                var initNamedProps = RuleUtil.MaybeLoadProp(eventData, inputs.InitNameQuery, inputs.InitName);
                var initProps = RuleUtil.MaybeLoadProp(eventData, inputs.InitialPropsQuery, inputs.InitialProps);
                string? initPropsJson = initProps?.ToString();

                var origin = sourceEvent.ToPreceding(Name);

                var resultId = await _formLogic.EventCreateForm(origin,
                    templateName,
                    workSet!.Value, workItem!.Value,
                    inputs.Home,
                    inputs.InitialTags,
                    initPropsJson, initNamedProps,
                    sealEvents,
                    trx);

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, resultId);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
