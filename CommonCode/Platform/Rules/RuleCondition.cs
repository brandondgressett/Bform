using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.Rules;

public enum QueryResult
{
    Any,
    None,
    Single
}

public class RuleCondition
{
    public string? Comment { get; set; }
    public List<RuleExpressionInvocation>? Append { get; set; } = new();

    [JsonProperty(Required = Required.Always)]
    public string Query { get; set; } = null!;

    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public QueryResult Check { get; set; } = QueryResult.Any;
    
    public bool Negate { get; set; } = false;

}
