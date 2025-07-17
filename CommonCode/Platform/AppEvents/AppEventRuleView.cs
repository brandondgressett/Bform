using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.AppEvents;

public class AppEventRuleView
{
    public JObject Data { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public Guid? ActionUser { get; set; }
    public JObject Appendix { get; set; } = new JObject();
    public List<string> EventTags { get; set; } = null!;
    public List<string> EntityTags { get; set; } = new();
    public string? EntityType { get; set; } 
    public string? EntityTemplate { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? HostWorkSet { get; set; }
    public Guid? HostWorkItem { get; set; }
        
}
