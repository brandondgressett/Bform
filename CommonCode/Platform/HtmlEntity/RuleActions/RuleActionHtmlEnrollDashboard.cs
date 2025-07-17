using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.Rules.RuleActions;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;

namespace BFormDomain.CommonCode.Platform.HtmlEntity.RuleActions;

public class RuleActionHtmlEnrollDashboard : RuleActionEntityEnrollDashboardBase
{
    public RuleActionHtmlEnrollDashboard(WorkSetLogic logic, IApplicationAlert alerts) : base(logic, alerts)
    {
    }

    public override string Name => RuleUtil.FixActionName(nameof(RuleActionHtmlEnrollDashboard));

    protected override string GetEntityRef(AppEventRuleView view, string? templateName, bool template, bool vm, string? query)
    {
        return HtmlEntityReferenceBuilderImplementation.MakeReference(view.EntityTemplate!, view.EntityId ?? Guid.Empty, template, vm, query).ToString();
    }
}
