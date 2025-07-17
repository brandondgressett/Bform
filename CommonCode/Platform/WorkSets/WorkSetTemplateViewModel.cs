using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetTemplateViewModel
{

    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int DescendingOrder { get; set; }
    public List<string> Tags { get; set; } = new List<string>();

    public WorkSetMenuItem? MenuItem { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public WorkSetHome Home { get; set; }
    public bool IsEveryoneAMember { get; set; }


    public static WorkSetTemplateViewModel Create(WorkSetTemplate template)
    {
        return new WorkSetTemplateViewModel
        {
            DescendingOrder = template.DescendingOrder,
            Title = template.Title,
            Tags = template.Tags,
            MenuItem = template.MenuItem,
            IsEveryoneAMember = template.IsEveryoneAMember,
            Name = template.Name,
            Home = template.Home
        };
    }

}
