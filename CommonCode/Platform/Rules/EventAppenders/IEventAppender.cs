using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.EventAppenders;

public interface IEventAppender
{
    string Name { get; }
    Task AddToAppendix(string? resultName, JObject eventData, JObject? appendArguments);
}
