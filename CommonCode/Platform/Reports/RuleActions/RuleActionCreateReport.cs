using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Reports.RuleActions;

public class RuleActionCreateReport : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly ReportLogic _reportLogic;

    public RuleActionCreateReport(IApplicationAlert alert, ReportLogic reportLogic)
    {
        _alerts = alert;
        _reportLogic = reportLogic;
    }


    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateReport));

    public class Arguments
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }

       
        public string? PreparedQueryData { get; set; }

        public JObject? ReportQueryData { get; set; }


        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }



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
        using (PerfTrack.Stopwatch(Name))
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

                var workSet = RuleUtil.MaybeLoadProp(eventData, inputs.WorkSetQuery, sourceEvent.HostWorkSet)!;
                if (workSet == null)
                    workSet = Constants.BuiltIn.SystemWorkSet;
                var workItem = RuleUtil.MaybeLoadProp(eventData, inputs.WorkItemQuery, sourceEvent.HostWorkItem)!;
                if (workItem == null)
                    workItem = Constants.BuiltIn.SystemWorkItem;

                var query = RuleUtil.MaybeLoadProp<JObject>(eventData, inputs.PreparedQueryData, inputs.ReportQueryData)!;
                query.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);

                var instanceId = 
                    await _reportLogic.EventCreateReport(
                        origin, 
                        templateName, 
                        workSet.Value, workItem.Value, 
                        query, 
                        eventTags, 
                        sealEvents, 
                        trx);
                
                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, instanceId);

            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                   LogLevel.Information, ex.TraceInformation());
                throw;
            }
        }
    }
}
