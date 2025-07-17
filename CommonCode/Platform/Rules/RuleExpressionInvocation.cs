using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules;

public class RuleExpressionInvocation
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = null!;
    public string? Result { get; set; }
    public JObject? Args { get; set; }
}

public class RuleActionInvocation 
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = null!;
    public string? Result { get; set; }
    public JObject? Args { get; set; }
    public bool SealEvents { get; set; }
}