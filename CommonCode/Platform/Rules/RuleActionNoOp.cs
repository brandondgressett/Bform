using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.Repository;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules;

public class RuleActionNoOp : IRuleActionEvaluator
{
    public string Name => RuleUtil.FixActionName(nameof(RuleActionNoOp));

    public Task Execute(ITransactionContext trx, string? result, JObject eventData, JObject? args, AppEvent sourceEvent, bool sealEvents, IEnumerable<string>? eventTags = null)
    {
        return Task.CompletedTask;
    }
}
