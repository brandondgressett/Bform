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

public class RuleActionAddWorkItemLink
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionAddWorkItemLink(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionAddWorkItemLink));

    public class Arguments
    {
        public string? IdQuery { get; set; }

        public string? Title { get; set; }
        public string? TitleQuery { get; set; }

        public string? LinkWorkSetQuery { get; set; }
        public string? LinkWorkItemQuery { get; set; }


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
        using (PerfTrack.Stopwatch(nameof(RuleActionAddWorkItemLink)))
        {
            try
            {
                args.Requires().IsNotNull();
                string resultProperty = "";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;


                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, null)!;
                id.Guarantees().IsNotNull();
                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title);
                title.Guarantees().IsNotNull();
                var linkWorkSet = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.LinkWorkSetQuery, null)!;
                linkWorkSet.Guarantees().IsNotNull();
                var linkWorkItem = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.LinkWorkItemQuery, null)!;
                linkWorkItem.Guarantees().IsNotNull();

                var origin = new AppEventOrigin(Name, null, null);
                await _logic.EventAddWorkItemLink(
                    origin, new WorkItemLink
                    {
                        Id = Guid.NewGuid(),
                        LinkCreated = DateTime.UtcNow,
                        Title = title!,
                        WorkItemId = linkWorkItem.Value,
                        WorkSetId = linkWorkSet.Value
                    }, id.Value, Constants.BuiltIn.SystemUser, trx,
                    sealEvents, eventTags);


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
