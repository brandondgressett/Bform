using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs.RuleActions;

public class RuleActionCreateKPI : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly KPILogic _logic;

    public RuleActionCreateKPI(
        KPILogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _logic = logic;
        _alerts = alerts;
        _terms = terms;
    }
    public string Name => RuleUtil.FixActionName(nameof(RuleActionCreateKPI));

    public class Arguments
    {
        public string? TemplateName { get; set; }
        public string? TemplateNameQuery { get; set; }

        public List<string>? InitialTags { get; set; }

        public List<string> WorkSetHostTags { get; set; } = null!;
        public List<string> WorkItemHostTags { get; set; } = null!;

        public string? QueryWorkSetHostTags { get; set; }
        public string? QueryWorkItemHostTags { get; set; }

        public List<string>? WorkSetSubjectTags { get; set; } = null!;
        public List<string>? WorkItemSubjectTags { get; set; } = null!;

        public string? QueryWorkSetSubjectTags { get; set; }
        public string? QueryWorkItemSubjectTags { get; set; }

        public string? QueryUserSubject { get; set; }

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
        using(PerfTrack.Stopwatch(nameof(RuleActionCreateKPI)))
        {
            try
            {
                string resultProperty = "CreatedKPI";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                args.Requires().IsNotNull();
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var templateName = RuleUtil.MaybeLoadProp(eventData, inputs.TemplateNameQuery, inputs.TemplateName)!;
                templateName.Guarantees().IsNotNullOrEmpty();

                var hostWSTags = RuleUtil.MaybeLoadProp(eventData, inputs.QueryWorkSetHostTags, inputs.WorkSetHostTags);
                var hostWITags = RuleUtil.MaybeLoadProp(eventData, inputs.QueryWorkItemHostTags, inputs.WorkItemHostTags);
                var subWSTags = RuleUtil.MaybeLoadProp(eventData, inputs.QueryWorkSetSubjectTags, inputs.WorkSetSubjectTags);
                var subWITags = RuleUtil.MaybeLoadProp(eventData, inputs.QueryWorkItemSubjectTags, inputs.WorkItemSubjectTags);
                Guid? userSubject = null!;
                if(!string.IsNullOrWhiteSpace(inputs.QueryUserSubject))
                    userSubject = RuleUtil.MaybeLoadProp(eventData, inputs.QueryUserSubject, userSubject);

                var origin = sourceEvent.ToPreceding(Name);


                var id = await _logic.EventCreateKPI(
                    origin, templateName, BuiltIn.SystemWorkSet, BuiltIn.SystemWorkItem,
                    hostWSTags, hostWITags, BuiltIn.SystemUser,
                    userSubject, null, null,
                    subWSTags, subWITags, inputs.InitialTags,
                    sealEvents,
                    trx);

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, id);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
        
    }


}
