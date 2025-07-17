using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems.RuleActions;

public class RuleActionWorkItemRemoveSectionEntity : IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly WorkItemLogic _logic;

    public RuleActionWorkItemRemoveSectionEntity(
        IApplicationAlert alert,
        IApplicationTerms terms,
        WorkItemLogic logic
        )
    {
        _alerts = alert;
        _terms = terms;
        _logic = logic;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionWorkItemRemoveSectionEntity));

    public class Arguments
    {
        public string? WorkItemIdQuery { get; set; }
        public int SectionId { get; set; }
        public string? SectionIdQuery { get; set; }

        public string? EntityUriQuery { get; set; }

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
        using (PerfTrack.Stopwatch(nameof(RuleActionWorkItemRemoveSectionEntity)))
        {
            try
            {
                args.Requires().IsNotNull();

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                var workItem = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.WorkItemIdQuery, null);
                workItem.Guarantees().IsNotNull();
                var sectionId = RuleUtil.MaybeLoadProp<int>(eventData, inputs.SectionIdQuery, inputs.SectionId);
                var entityIdStr = RuleUtil.MaybeLoadProp(eventData, inputs.EntityUriQuery, string.Empty)!;
                entityIdStr.Guarantees().IsNotNullOrEmpty();
                var uri = new Uri(entityIdStr);

                var origin = new AppEventOrigin(Name, null, null);

                await _logic.EventRemoveSectionEntity(
                    origin, workItem!.Value, sectionId, 
                    uri, Constants.BuiltIn.SystemUser, 
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
