using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.Repository;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules;

public interface IRuleActionEvaluator
{
    string Name { get; }

    Task Execute(ITransactionContext trx, string? result, JObject eventData, JObject? args, AppEvent sourceEvent, bool sealEvents, IEnumerable<string>? eventTags = null);
    
}
