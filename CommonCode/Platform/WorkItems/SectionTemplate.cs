using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class SectionTemplate
{
    public int Id { get; set; }
    public int DescendingOrder { get; set; }
    public string Renderer { get; set; } = null!;

    public bool IsCreateOnNew { get; set; }
    public bool IsCreateOnDemand { get; set; }
    
    public bool IsEntityList { get; set; }

    public string? EntityTemplateName { get; set; } = null!;
    
    public JObject? CreationData { get; set; }

    public ProcessInstanceCommand? NewInstanceProcess { get; set; }

}

