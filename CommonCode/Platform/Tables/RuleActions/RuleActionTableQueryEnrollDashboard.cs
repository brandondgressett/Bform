using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.Rules.RuleActions;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;

namespace BFormDomain.CommonCode.Platform.Tables.RuleActions;

public class RuleActionTableQueryEnrollDashboard : RuleActionEntityEnrollDashboardBase
{
    public RuleActionTableQueryEnrollDashboard(WorkSetLogic logic, IApplicationAlert alerts) : base(logic, alerts)
    {
    }

    public override string Name => RuleUtil.FixActionName(nameof(RuleActionTableQueryEnrollDashboard));

    protected override string GetEntityRef(AppEventRuleView view, string? templateName, bool template, bool vm, string? query)
    {
        return TableEntityReferenceBuilderImplementation.MakeReference(view.EntityTemplate!, view.EntityId ?? Guid.Empty, template, vm, query).ToString();
    }
}
