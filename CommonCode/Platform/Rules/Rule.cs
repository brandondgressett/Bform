using BFormDomain.CommonCode.Platform.Content;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Platform.Rules;

public class Rule: IContentType
{
    /// <summary>
    /// Name and id, must be unique among rules.
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = "";

    public string? Comment { get; set; }


    [JsonProperty(Required = Required.Always)]
    public List<string> TopicBindings { get; set; } = new();
        
    public int DescendingOrder { get; set; }


    public string? DomainName { get; set; } = nameof(Rule);
    public Dictionary<string, string>? SatelliteData { get; set; } = new();

    public List<string> Tags {get; set;} = new();

    public List<string> EventTags { get; set; } = new();

    [JsonProperty(Required = Required.Always)]
    public List<RuleCondition> Conditions { get; set; } = new();

    [JsonProperty(Required = Required.Always)]
    public List<RuleAction> Actions { get; set; } = new();


}
