using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.Repository;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.RuleActions;

public class RuleActionLogEventData : IRuleActionEvaluator
{
    private readonly ILogger<RuleActionLogEventData> _logger;

    public RuleActionLogEventData(ILogger<RuleActionLogEventData> logger)
    {
        _logger = logger;
    }
    public string Name => RuleUtil.FixActionName(nameof(RuleActionLogEventData));

    public Task Execute(ITransactionContext trx, string? result, JObject eventData, JObject? args, AppEvent sourceEvent, bool sealEvents, IEnumerable<string>? eventTags = null)
    {
        _logger.LogInformation("{appevent}", eventData);
        return Task.CompletedTask;
    }
}
