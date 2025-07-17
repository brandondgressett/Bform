using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.RuleActions;

public abstract class RuleActionEntityEnrollDashboardBase : IRuleActionEvaluator
{
    protected readonly IApplicationAlert _alerts;
    protected readonly WorkSetLogic _logic;

    public RuleActionEntityEnrollDashboardBase(WorkSetLogic logic, IApplicationAlert alerts)
    {
        _logic = logic;
        _alerts = alerts;
    }

    public abstract string Name { get; }

    public class Arguments
    {
        public bool WantsVM { get; set; }
        public bool WantsTemplate { get; set; }

        public string? QueryStringQuery { get; set; }
        public string? QueryString { get; set; } = "";

        public string? ScoreQuery { get; set; }

        public int Score { get; set; }

        public string? GroupingQuery { get; set; }
        public string? Grouping { get; set; }

        public string? OrderQuery { get; set; }
        public int Order { get; set; }

        public string? MetaTagsQuery { get; set; }

        public List<string> MetaTags { get; set; } = new();
    }

    protected abstract string GetEntityRef(AppEventRuleView view, string? templateName, bool template, bool vm, string? query);

    public async Task Execute(
        ITransactionContext trx, 
        string? result, 
        JObject eventData, 
        JObject? args, 
        AppEvent sourceEvent, 
        bool sealEvents, 
        IEnumerable<string>? eventTags = null)
    {
        try
        {
            args.Requires().IsNotNull();
            var inputs = args!.ToObject<Arguments>()!;
            inputs.Guarantees().IsNotNull();

            var score = RuleUtil.MaybeLoadProp(eventData, inputs.ScoreQuery, inputs.Score);
            var group = RuleUtil.MaybeLoadProp(eventData, inputs.GroupingQuery, inputs.Grouping);
            var order = RuleUtil.MaybeLoadProp(eventData, inputs.OrderQuery, inputs.Order);
            var metaTags = RuleUtil.MaybeLoadProp(eventData, inputs.MetaTagsQuery, inputs.MetaTags);
            var queryString = RuleUtil.MaybeLoadProp(eventData, inputs.QueryStringQuery, inputs.QueryString);

            var ruleView = eventData.ToObject<AppEventRuleView>()!;
            ruleView.Guarantees().IsNotNull();

            var uri = GetEntityRef(ruleView, ruleView.EntityTemplate, inputs.WantsTemplate, inputs.WantsVM, queryString);

            var candidate = new DashboardCandidate
            {
                Id = Guid.NewGuid(),
                WorkSet = ruleView.HostWorkSet!.Value,
                Score = score,
                DescendingOrder = order,
                Grouping = group,
                Created = DateTime.UtcNow,
                IsWinner = false,
                EntityRef = uri,
                EntityType = ruleView.EntityType!,
                TemplateName = ruleView.EntityTemplate,
                Tags = ruleView.EntityTags,
                MetaTags = metaTags ?? new(),
                Version = 0
            };

            await _logic.EnrollEntityIntoDashboard(trx, candidate);

        } catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
        }
    }
}
