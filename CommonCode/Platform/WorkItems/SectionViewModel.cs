using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkItems;

public class SectionViewModel: SectionTemplate
{
    public List<JObject> SectionData { get; set; } = null!;




}
