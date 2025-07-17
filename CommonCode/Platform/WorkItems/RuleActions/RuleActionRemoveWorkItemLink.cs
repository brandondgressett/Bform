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

public class RuleActionRemoveWorkItemLink
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionRemoveWorkItemLink(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionRemoveWorkItemLink));

    public class Arguments
    {
        public string? WorkItemIdQuery { get; set; }
        public string? LinkIdQuery { get; set; }
        
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
        using (PerfTrack.Stopwatch(nameof(RuleActionRemoveWorkItemLink)))
        {
            try
            {
                args.Requires().IsNotNull();
                
                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var workItem = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.WorkItemIdQuery, null);
                workItem.Guarantees().IsNotNull();
                var link = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.LinkIdQuery, null);
                link.Guarantees().IsNotNull();

                var origin = new AppEventOrigin(Name, null, null);

                await _logic.EventRemoveWorkItemLink(
                    origin, 
                    link!.Value, workItem!.Value, 
                    Constants.BuiltIn.SystemUser, trx, sealEvents, eventTags);


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
