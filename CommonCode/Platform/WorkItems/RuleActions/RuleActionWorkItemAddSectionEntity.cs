using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems.RuleActions;

public class RuleActionWorkItemAddSectionEntity
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionWorkItemAddSectionEntity(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionWorkItemAddSectionEntity));

    public class Arguments
    {
        public string IdQuery { get; set; } = null!;

        public string? SectionIdQuery { get; set; }
        public int SectionId { get; set; }

        public string? CreationDataQuery { get; set; }
        public JObject? CreationData { get; set; }
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
        using (PerfTrack.Stopwatch(nameof(RuleActionWorkItemAddSectionEntity)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;
                var templateResult = resultProperty + "Template";
              
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, null)!;
                id.Guarantees().IsNotNull();
                var sectionId = RuleUtil.MaybeLoadProp(eventData, inputs.SectionIdQuery, inputs.SectionId);
                var creationData = RuleUtil.MaybeLoadProp(eventData, inputs.CreationDataQuery, inputs.CreationData);
                creationData.Guarantees().IsNotNull();

                var origin = new AppEventOrigin(Name, null, null);

                var (wi, wit) = await _logic.EventAddSection(origin, id.Value, sectionId, creationData,
                    Constants.BuiltIn.SystemUser, trx, sealEvents, eventTags);

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, JObject.FromObject(wi));
                appendix.Add(templateResult, JObject.FromObject(wit));


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
