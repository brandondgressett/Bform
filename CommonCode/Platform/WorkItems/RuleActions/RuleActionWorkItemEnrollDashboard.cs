using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.Rules.RuleActions;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;

namespace BFormDomain.CommonCode.Platform.WorkItems.RuleActions;

public class RuleActionWorkItemEnrollDashboard : RuleActionEntityEnrollDashboardBase
{
    public RuleActionWorkItemEnrollDashboard(WorkSetLogic logic, IApplicationAlert alerts) : base(logic, alerts)
    {
    }

    public override string Name => RuleUtil.FixActionName(nameof(RuleActionWorkItemEnrollDashboard));

    protected override string GetEntityRef(AppEventRuleView view, string? templateName, bool template, bool vm, string? query)
    {
        return WorkItemReferenceBuilderImplementation.MakeReference(view.EntityTemplate!, view.EntityId ?? Guid.Empty, template, vm, query).ToString();
    }
}
