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

public class RuleActionAddWorkItemBookmark
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionAddWorkItemBookmark(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionAddWorkItemBookmark));

    public class Arguments
    {
        public string? IdQuery { get; set; }
        public string? Title { get; set; }
        public string? TitleQuery { get; set; }

        public string? UserAssigneeQuery { get; set; }
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
        using (PerfTrack.Stopwatch(nameof(RuleActionAddWorkItemBookmark)))
        {
            try
            {
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var workItem = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, null)!;
                workItem.Guarantees().IsNotNull();
                var title = RuleUtil.MaybeLoadProp(eventData, inputs.TitleQuery, inputs.Title)!;
                title.Guarantees().IsNotNullOrEmpty();
                var userAssignee = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.UserAssigneeQuery, null);
                userAssignee.Guarantees().IsNotNull();

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventAddWorkItemBookmark(origin, new WorkItemBookmark
                {
                    Title = title,
                    Created = DateTime.UtcNow,
                    ApplicationUser = userAssignee!.Value
                }, workItem!.Value, userAssignee!.Value,
                trx, sealEvents, eventTags);

                


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
