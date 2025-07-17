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

public class RuleActionCreateForms : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly FormLogic _formLogic;

    public RuleActionCreateForms(
        FormLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _formLogic = logic;
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateForms));

    public class FormCreateArgs
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }

        public List<string>? InitialTags { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FormInstanceHome Home { get; set; }

        public string? InitName { get; set; }
        public string? InitNameQuery { get; set; }
    }

    public class Arguments
    {
        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }

        public List<FormCreateArgs> Create { get; set; } = new();
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
        using (PerfTrack.Stopwatch(nameof(RuleActionCreateForms)))
        {
            try
            {
                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var workSet = RuleUtil.MaybeLoadProp(eventData, inputs.WorkSetQuery, sourceEvent.HostWorkSet);
                var workItem = RuleUtil.MaybeLoadProp(eventData, inputs.WorkItemQuery, sourceEvent.HostWorkItem);

                var commands = new List<CreateFormInstancesCommand>();
                foreach(var instance in inputs.Create)
                {

                    var templateName = RuleUtil.MaybeLoadProp(eventData, instance.TemplateNameQuery, instance.TemplateName)!;
                    templateName.Guarantees().IsNotNullOrEmpty();

                    var initProps = RuleUtil.MaybeLoadProp(eventData, instance.InitNameQuery, instance.InitName);


                    commands.Add(new CreateFormInstancesCommand
                    {
                        Home = instance.Home,
                        TemplateName = templateName,
                        InitialPropertiesName = initProps,
                        InitialTags = instance.InitialTags
                    });
                }

                var origin = sourceEvent.ToPreceding(Name);

                await _formLogic.EventCreateManyForms(
                    commands,
                    origin,
                    workSet!.Value, workItem!.Value,
                    sealEvents,
                    trx);
                    


            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }

        }
    }
}
